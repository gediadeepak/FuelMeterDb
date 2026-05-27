// Shared client helpers for FuelMeter pages.

// ---------- Generic file download (used by ExportImport) ----------
window.downloadFile = function (fileName, base64Content, mimeType) {
    try {
        const link = document.createElement('a');
        link.href = `data:${mimeType};base64,${base64Content}`;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    } catch (e) {
        console.error('downloadFile failed', e);
    }
};

// ---------- Camera OCR using Tesseract.js (CDN, lazy) ----------
window.fuelMeterOcr = (function () {
    let tesseractLoading = null;

    function loadScript(src) {
        return new Promise((resolve, reject) => {
            const existing = document.querySelector(`script[data-fm="${src}"]`);
            if (existing) { resolve(); return; }
            const s = document.createElement('script');
            s.src = src;
            s.setAttribute('data-fm', src);
            s.onload = () => resolve();
            s.onerror = () => reject(new Error('Failed to load ' + src));
            document.head.appendChild(s);
        });
    }

    async function ensureTesseract() {
        if (window.Tesseract) return;
        if (!tesseractLoading) {
            tesseractLoading = loadScript('https://cdn.jsdelivr.net/npm/tesseract.js@5/dist/tesseract.min.js');
        }
        await tesseractLoading;
    }

    async function recognizeMeter(imageDataUrl) {
        try {
            await ensureTesseract();
            const { data } = await window.Tesseract.recognize(imageDataUrl, 'eng', {
                tessedit_char_whitelist: '0123456789'
            });
            const raw = (data && data.text) ? data.text : '';
            const matches = raw.match(/\d+/g) || [];
            if (matches.length === 0) return null;
            matches.sort((a, b) => b.length - a.length);
            return matches[0];
        } catch (err) {
            console.error('OCR failed', err);
            return null;
        }
    }

    return { recognizeMeter };
})();

// ---------- Monthly Statement: PDF + Print ----------
// Uses jsPDF directly (no html2canvas) for maximum compatibility in MAUI WebView.
window.fuelMeterStatement = (function () {
    let jsPdfLoading = null;

    function loadScript(src) {
        return new Promise((resolve, reject) => {
            const existing = document.querySelector(`script[data-fm="${src}"]`);
            if (existing) { resolve(); return; }
            const s = document.createElement('script');
            s.src = src;
            s.setAttribute('data-fm', src);
            s.onload = () => resolve();
            s.onerror = () => reject(new Error('Failed to load ' + src));
            document.head.appendChild(s);
        });
    }

    async function ensureJsPdf() {
        if (window.jspdf && window.jspdf.jsPDF) return;
        if (!jsPdfLoading) {
            jsPdfLoading = loadScript('https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js');
        }
        await jsPdfLoading;
    }

    async function downloadPdf(payload, fileName) {
        try {
            await ensureJsPdf();
            const { jsPDF } = window.jspdf;
            const doc = new jsPDF('p', 'mm', 'a4');
            const left = 15;
            let y = 20;

            doc.setFont('helvetica', 'bold');
            doc.setFontSize(18);
            doc.setTextColor(25, 118, 210);
            doc.text('FuelMeter Monthly Statement', left, y);

            y += 10;
            doc.setDrawColor(25, 118, 210);
            doc.setLineWidth(0.6);
            doc.line(left, y, 195, y);

            y += 8;
            doc.setFont('helvetica', 'normal');
            doc.setFontSize(11);
            doc.setTextColor(60, 60, 60);

            const meta = [
                ['Account',   payload.account  || '-'],
                ['Email',     payload.email    || '-'],
                ['Period',    payload.period   || '-'],
                ['Generated', payload.generated|| '-']
            ];
            meta.forEach(([k, v]) => {
                doc.setFont('helvetica', 'bold');
                doc.text(`${k}:`, left, y);
                doc.setFont('helvetica', 'normal');
                doc.text(String(v), left + 28, y);
                y += 7;
            });

            y += 6;
            doc.setFont('helvetica', 'bold');
            doc.setFontSize(13);
            doc.setTextColor(33, 33, 33);
            doc.text('Summary', left, y);

            y += 4;
            doc.setDrawColor(220, 220, 220);
            doc.line(left, y, 195, y);
            y += 7;

            doc.setFontSize(11);
            doc.setFont('helvetica', 'bold');
            doc.text('Fuel',           left,       y);
            doc.text('Units Used',     left + 70,  y);
            doc.text('Estimated Cost', left + 120, y);
            y += 3;
            doc.line(left, y, 195, y);
            y += 7;

            doc.setFont('helvetica', 'normal');
            const cur = payload.currency || '£';
            const rows = [
                ['Electricity', payload.electricity?.usage ?? '-', payload.electricity?.cost ?? '-'],
                ['Gas',         payload.gas?.usage ?? '-',         payload.gas?.cost ?? '-']
            ];
            rows.forEach(r => {
                doc.text(String(r[0]), left,       y);
                doc.text(String(r[1]), left + 70,  y);
                doc.text(`${cur}${r[2]}`, left + 120, y);
                y += 7;
            });

            y += 2;
            doc.line(left, y, 195, y);
            y += 8;

            doc.setFont('helvetica', 'bold');
            doc.text('Total',                 left,       y);
            doc.text(`${cur}${payload.total ?? '-'}`, left + 120, y);

            y += 18;
            doc.setFont('helvetica', 'italic');
            doc.setFontSize(9);
            doc.setTextColor(120, 120, 120);
            const disclaimer =
                'This statement is an estimate generated from meter readings recorded in ' +
                'FuelMeter and the user-provided tariff. Always confirm with your supplier\'s ' +
                'official bill.';
            const wrapped = doc.splitTextToSize(disclaimer, 180);
            doc.text(wrapped, left, y);

            doc.save(fileName || 'FuelMeter-Statement.pdf');
            return true;
        } catch (err) {
            console.error('PDF generation failed', err);
            return false;
        }
    }

    function print(payload) {
        try {
            const cur = payload.currency || '£';
            const w = window.open('', '_blank');
            if (!w) return false;
            w.document.write(`
                <html><head><title>FuelMeter Statement</title>
                <style>
                  body { font-family: Arial, sans-serif; color: #222; padding: 28px; }
                  h1 { color: #1976d2; margin: 0 0 12px; }
                  .meta { display: grid; grid-template-columns: 1fr 1fr; gap: 4px 16px; margin: 12px 0 24px; }
                  table { width: 100%; border-collapse: collapse; }
                  th, td { padding: 8px 10px; border-bottom: 1px solid #ddd; text-align: left; }
                  th { background: #fafafa; }
                  .total { font-weight: bold; border-top: 2px solid #333; }
                  .disclaimer { margin-top: 24px; font-size: 11px; font-style: italic; color: #777; }
                </style></head><body>
                  <h1>FuelMeter Monthly Statement</h1>
                  <div class="meta">
                    <div><strong>Account:</strong> ${payload.account || '-'}</div>
                    <div><strong>Email:</strong> ${payload.email || '-'}</div>
                    <div><strong>Period:</strong> ${payload.period || '-'}</div>
                    <div><strong>Generated:</strong> ${payload.generated || '-'}</div>
                  </div>
                  <table>
                    <thead><tr><th>Fuel</th><th>Units Used</th><th>Estimated Cost</th></tr></thead>
                    <tbody>
                      <tr><td>Electricity</td><td>${payload.electricity?.usage ?? '-'}</td><td>${cur}${payload.electricity?.cost ?? '-'}</td></tr>
                      <tr><td>Gas</td><td>${payload.gas?.usage ?? '-'}</td><td>${cur}${payload.gas?.cost ?? '-'}</td></tr>
                      <tr class="total"><td>Total</td><td></td><td>${cur}${payload.total ?? '-'}</td></tr>
                    </tbody>
                  </table>
                  <p class="disclaimer">This statement is an estimate generated from meter readings
                    recorded in FuelMeter and the user-provided tariff. Always confirm with your
                    supplier's official bill.</p>
                </body></html>
            `);
            w.document.close();
            w.focus();
            setTimeout(() => { try { w.print(); } catch (e) {} }, 300);
            return true;
        } catch (err) {
            console.error('Print failed', err);
            return false;
        }
    }

    return { downloadPdf, print };
})();
