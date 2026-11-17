# FuelMeter Google Play submission

## Build the app bundle

1. Open [FuelMeter Actions](https://github.com/gediadeepak/FuelMeterDb/actions) and select **Build Google Play App Bundle → Run workflow** on `main`.
2. Enter a display version such as `1.0.0` and a version code greater than every version code already uploaded to Play. Use `1` for the first upload if it has never been used.
3. Download the run's artifact, for example **FuelMeter-PlayStore-1**, unzip it, and upload `FuelMeter.aab` to an internal test track in Play Console.

The package is `com.fuelmeter.app`. Configure `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD` and `ANDROID_KEYSTORE_PASSWORD` as repository Actions secrets. Use the upload key registered in Play Console if the package has already been enrolled in Play App Signing. [Play App Signing](https://support.google.com/googleplay/android-developer/answer/9842756)

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

## App content

- **Privacy policy** — [FuelMeter Privacy Policy](https://fuelmeter.shrijiitservices.com/_content/FuelMeter.Components/privacy-policy.html)
- **Account deletion URL** — [Delete your FuelMeter account](https://fuelmeter.shrijiitservices.com/_content/FuelMeter.Components/account-deletion.html). In the app use **Settings → Delete account**. Deploy both pages, open them signed out, and complete deletion with a disposable account before entering the URLs. [Google Play account deletion requirements](https://support.google.com/googleplay/android-developer/answer/13327111)
- **App access** — Give reviewers a working test account with sample data and instructions to find Dashboard, **Add meter reading**, and **Settings → Delete account**. Verify access on a fresh installation and keep a separate account for testing deletion so the review account stays usable. [Preparing for review](https://support.google.com/googleplay/android-developer/answer/9859455)
- **Data safety** — Start with required name, email address and account/user ID for account management. Include optional user-entered meter readings, notes, tariff and budget data under the relevant Play categories. Include optional photos sent to OpenAI for meter scanning and device or other IDs for Firebase budget alerts if enabled. Confirm server access-log and SDK collection, plus provider uses and retention, before choosing sharing and ephemeral processing answers. [Data safety definitions](https://support.google.com/googleplay/android-developer/answer/10787469)
- **Other declarations** — Complete Ads, Target audience and content, Content rating, and any other declarations requested in Play Console. The current app does not display ads. Meter scanning suggests values from user photos and requires the user to verify them. Test with the actual configuration used for review.

## Production gates

- Review the hosting logs for personal data, document why it is required, and set and verify a deletion or anonymisation schedule. The policy discloses hosting in India and that service logs have no fixed expiry. Confirm applicable UK transfer safeguards with the hosting, email and support providers, and verify the other providers' processing locations and retention. Update the policy and Data safety answers before submission. [ICO storage limitation guidance](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/data-protection-principles/a-guide-to-the-data-protection-principles/storage-limitation/) · [ICO international transfers guide](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/international-transfers/a-brief-guide-to-international-transfers/)
- Smoke-test registration, login, password reset, camera and photo picker, manual and automatic readings, notification permissions, and account deletion with the test-track build. Check Play Console's pre-launch report and complete any developer verification requested.
- For personal Play Console accounts created after 13 November 2023, run a closed test with at least 12 continuously opted-in testers for 14 days before applying for production access. [Google Play testing requirements](https://support.google.com/googleplay/android-developer/answer/14151465)
