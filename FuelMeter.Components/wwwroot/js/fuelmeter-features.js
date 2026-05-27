// Camera OCR (uses Tesseract.js loaded from CDN) and PDF/Print helpers for
// Monthly Statement. Both modules are safe no-ops if the network is unavailable.

window.fuelMeterOcr = (function () {
    let tesseractLoading = null;

    function loadScript(src) {
        return new Promise((resolve, reject) => {
            const existing = document.querySelector(`script[src="${src}"]`);
            if (existing) { resolve(); return; }
            const s = document.createElement('script');
            s.src = src;
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
            // Keep digits only; pick the longest digit run
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

window.fuelMeterStatement = (function () {
    let jsPdfLoading = null;
    let html2canvasLoading = null;

    function loadScript(src) {
        return new Promise((resolve, reject) => {
            const existing = document.querySelector(`script[src="${src}"]`);
            if (existing) { resolve(); return; }
            const s = document.createElement('script');
            s.src = src;
            s.onload = () => resolve();
            s.onerror = () => reject(new Error('Failed to load ' + src));
            document.head.appendChild(s);
        });
    }

    async function ensureLibs() {
        if (!window.jspdf) {
            if (!jsPdfLoading) {
                jsPdfLoading = loadScript('https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js');
            }
            await jsPdfLoading;
        }
        if (!window.html2canvas) {
            if (!html2canvasLoading) {
                html2canvasLoading = loadScript('https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js');
            }
            await html2canvasLoading;
        }
    }

    async function downloadPdf(elementId, fileName) {
        const el = document.getElementById(elementId);
        if (!el) return;
        try {
            await ensureLibs();
            const canvas = await window.html2canvas(el, { scale: 2, backgroundColor: '#ffffff' });
            const imgData = canvas.toDataURL('image/png');
            const { jsPDF } = window.jspdf;
            const pdf = new jsPDF('p', 'mm', 'a4');
            const pageWidth = pdf.internal.pageSize.getWidth();
            const pageHeight = pdf.internal.pageSize.getHeight();
            const imgWidth = pageWidth - 20;
            const imgHeight = (canvas.height * imgWidth) / canvas.width;
            let position = 10;
            let heightLeft = imgHeight;

            pdf.addImage(imgData, 'PNG', 10, position, imgWidth, imgHeight);
            heightLeft -= (pageHeight - 20);
            while (heightLeft > 0) {
                pdf.addPage();
                position = 10 - (imgHeight - heightLeft);
                pdf.addImage(imgData, 'PNG', 10, position, imgWidth, imgHeight);
                heightLeft -= (pageHeight - 20);
            }
            pdf.save(fileName || 'FuelMeter-Statement.pdf');
        } catch (err) {
            console.error('PDF generation failed, falling back to print', err);
            print(elementId);
        }
    }

    function print(elementId) {
        const el = document.getElementById(elementId);
        if (!el) return;
        const w = window.open('', '_blank');
        if (!w) return;
        w.document.write(`
            <html><head><title>FuelMeter Statement</title>
            <style>
              body { font-family: Arial, sans-serif; color: #222; padding: 24px; }
              table { width: 100%; border-collapse: collapse; }
              th, td { padding: 8px 10px; border-bottom: 1px solid #ddd; text-align: left; }
              th { background: #fafafa; }
              h2,h3 { margin: 0 0 8px; }
            </style>
            </head><body>${el.innerHTML}</body></html>
        `);
        w.document.close();
        w.focus();
        setTimeout(() => { w.print(); w.close(); }, 250);
    }

    return { downloadPdf, print };
})();
