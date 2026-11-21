# FuelMeter Google Play submission

## Build the app bundle

1. Open [FuelMeter Actions](https://github.com/gediadeepak/FuelMeterDb/actions) and select **Build Google Play App Bundle → Run workflow** on `codex/subscriptions-and-scan-credits`, which contains the subscription release.
2. Enter a display version such as `1.0.0` and a version code greater than every version code already uploaded to Play. Use `1` for the first upload if it has never been used.
3. Download the run's artifact, for example **FuelMeter-PlayStore-1**, unzip it, and upload `FuelMeter.aab` to an internal test track in Play Console.

The package is `com.fuelmeter.app`. Configure `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD` and `ANDROID_KEYSTORE_PASSWORD` as repository Actions secrets. Use the upload key registered in Play Console if the package has already been enrolled in Play App Signing. [Play App Signing](https://support.google.com/googleplay/android-developer/answer/9842756)

The signed bundle built on 21 November 2026 has version `1.0.0`, version code `20261121`, and is available from [build 44896337268](https://github.com/gediadeepak/FuelMeterDb/actions/runs/44896337268). Download the `FuelMeter-PlayStore-20261121` artifact and extract `FuelMeter.aab`. Future uploads must use a greater version code. Signature, package identity, target API 36, and native 16 KB alignment checks passed for this bundle.

Its upload certificate SHA-256 fingerprint is `99:8C:3D:CB:8D:2B:F1:59:01:A4:D8:83:F9:78:1A:A7:14:86:1A:78:86:80:CE:92:71:76:F7:A6:E8:B7:5C:B6`. Play Console's app-signing certificate is managed separately by Google. Upload-certificate matching was not confirmed from the current Console view, so verify that Play accepts the bundle before sharing the test release.

## Store listing

### App name

FuelMeter

### Short description

Track gas and electricity readings, usage and estimated energy costs.

### Full description

FuelMeter helps you track your household gas and electricity use. Enter readings manually or scan a meter photo to get a suggested reading, then check the value before you save it.

See how your usage changes over time, enter your tariff to estimate costs, set budgets, and schedule reminders to enter new readings. You can export your records from the app.

### Support email

support@shrijiitservices.com

Upload a 512 × 512 store icon, a 1024 × 500 feature graphic, and at least two screenshots from the actual Android app. Use sample meter data and avoid showing real account information. [Google Play preview asset specs](https://support.google.com/googleplay/android-developer/answer/9866151)

The prepared icon and feature graphic are in `store-assets/`. Android screenshots remain required. The feature graphic uses an illustrative chart and the existing FuelMeter branding.

Set the app as **free to download**, with optional in-app subscriptions. The intended audience is **18 and over**, confirmed by the owner. **House & home** is the proposed category for its household energy tracking features.

## App content

- **Privacy policy** — [FuelMeter Privacy Policy](https://fuelmeter.shrijiitservices.com/_content/FuelMeter.Components/privacy-policy.html)
- **Account deletion URL** — [Delete your FuelMeter account](https://fuelmeter.shrijiitservices.com/_content/FuelMeter.Components/account-deletion.html). In the app use **Settings → Delete account**. Deploy both pages, open them signed out, and complete deletion with a disposable account before entering the URLs. [Google Play account deletion requirements](https://support.google.com/googleplay/android-developer/answer/13327111)
- **App access** — Give reviewers a working test account with sample data and instructions to find Dashboard, **Add meter reading**, and **Settings → Delete account**. Verify access on a fresh installation and keep a separate account for testing deletion so the review account stays usable. [Preparing for review](https://support.google.com/googleplay/android-developer/answer/9859455)
- **Data safety** — Include required name, email address and account/user ID for account management. Include optional user-entered meter readings, notes, tariff and budget data under the relevant Play categories, plus purchase history when subscriptions are purchased. Include optional photos sent to OpenAI for meter scanning and device or other IDs for Firebase budget alerts if enabled. Disclose the retained protected trial identity after deletion. Confirm server access-log and SDK collection, plus provider uses and retention, before choosing sharing and ephemeral processing answers. [Data safety definitions](https://support.google.com/googleplay/android-developer/answer/10787469)
- **Other declarations** — Complete Ads, Target audience and content, Content rating, and any other declarations requested in Play Console. The current app does not display ads. Meter scanning suggests values from user photos and requires the user to verify them. Test with the actual configuration used for review.

## Production gates

Password-reset email links use `https://fuelmeter.shrijiitservices.com/reset-password?token=…`. Android builds include a verified intent filter for that exact path. The website serves `/.well-known/assetlinks.json` with the Play app-signing SHA-256 fingerprint copied from the Console's Digital Asset Links snippet and the release APK signing fingerprint. Recheck this file after a signing-key upgrade. The shared router queues validated links during startup and navigates directly to password reset without requiring sign-in. The web reset page remains available and offers an explicit Open in FuelMeter button in Android browsers. These changes require an updated Android installation; older builds have no app-link handler. No Android device was connected during development, so verify cold-start and already-running reset-link taps with the new Play test-track build.

The URL handler accepts only HTTPS, the FuelMeter host, the reset route and a single correctly formed token. It does not save or log tokens. Android captures cold-start links before window creation, uses the reset path as the WebView's start path and reuses its activity for warm links. The router retains the pending link until the reset screen acknowledges it and waits for the asynchronous WebView location update before rendering login or dashboard. Component tests cover delayed navigation, remembered sessions, warm links and login requests completing after the reset link arrives. iOS universal-link entitlement and Apple site association have not been configured; this release enables Android App Links.

- Configure the Google Play product `fuelmeter_premium_monthly`, monthly auto-renewing base plan `monthly`, UK availability and £1.99 consumer price. Complete the merchant payments profile, backend Google Play credentials and authenticated notifications, then verify purchase and restore through license testing. Details are in [subscription setup](subscriptions.md). The store product had not yet been created when the owner confirmed this on 21 November 2026.
- Play Console shows FuelMeter as a draft. Its privacy policy URL was saved to the draft on 21 November 2026. Production access is currently locked and the dashboard reports zero closed-test testers. Complete the store setup and required testing before requesting production access.
- Review the hosting logs for personal data, document why it is required, and set and verify a deletion or anonymisation schedule. The policy discloses hosting in India and that service logs have no fixed expiry. Confirm applicable UK transfer safeguards with the hosting, email and support providers, and verify the other providers' processing locations and retention. Update the policy and Data safety answers before submission. [ICO storage limitation guidance](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/data-protection-principles/a-guide-to-the-data-protection-principles/storage-limitation/) · [ICO international transfers guide](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/international-transfers/a-brief-guide-to-international-transfers/)
- Smoke-test registration, login, password reset, camera and photo picker, manual and automatic readings, notification permissions, and account deletion with the test-track build. Check Play Console's pre-launch report and complete any developer verification requested.
- For personal Play Console accounts created after 13 November 2023, run a closed test with at least 12 continuously opted-in testers for 14 days before applying for production access. [Google Play testing requirements](https://support.google.com/googleplay/android-developer/answer/14151465)
