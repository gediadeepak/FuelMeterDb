# FuelMeter subscriptions

Subscription development is on `codex/subscriptions-and-scan-credits`. The combined deployment workflow supports this branch and `main`.

## Settings and access

All subscription business settings are rows in `ApplicationSettings`, with `Key`, `Value` and `UpdatedAtUtc`. Changes are read on the next API request. Existing per-account meter and reminder preferences retain their existing storage.

| Setting | Initial value |
| --- | --- |
| `Subscription.Android.PurchasesEnabled` | `true`, effective after Google server credentials are configured |
| `Subscription.Android.PremiumAccessEnabled` | `true` |
| `Subscription.Web.PurchasesEnabled` | `false` |
| `Subscription.Web.PremiumAccessEnabled` | `true` |
| `Subscription.Windows.PurchasesEnabled` | `false` |
| `Subscription.Windows.PremiumAccessEnabled` | `true` |
| `Subscription.iOS.PurchasesEnabled` | `false` |
| `Subscription.iOS.PremiumAccessEnabled` | `false` |
| `Subscription.Scans.Monthly` | `30` |
| `Subscription.Scans.Daily` | `5` |
| `Subscription.Scans.AttemptsDaily` | `10` |
| `Subscription.Scans.AttemptsMonthly` | `60` |
| `Subscription.Scans.TimeZone` | `Europe/London` |
| `Subscription.Trial.Hours` | `24` |
| `Subscription.Trial.Scans` | `1` |
| `Subscription.Trial.RetentionYears` | `1` |
| `Subscription.Google.PackageName` | `com.fuelmeter.app` |
| `Subscription.Google.ProductId` | `fuelmeter_premium_monthly` |
| `Subscription.Google.BasePlanId` | `monthly` |
| `Subscription.PriceGBP` | `1.99` planning reference, checkout displays Google's actual GBP price |

The daily allowance is a provisional operational choice pending real token-usage measurements. Paid scan periods follow the verified Google subscription billing period. Complimentary access uses UK calendar months. Trials have a single allowance for their 24-hour lifetime. Successful scans and live reservations count against both daily and period limits. Failed scans release successful-scan credits but count towards the retry allowance.

Only entitled accounts see AI scanning. Manual meter entry stays available. Access is checked on the API, independently of the platform flags sent by a client. iOS has no active purchase flow. An account's valid Google entitlement is shared across Android, web and Windows.

## Owner access

The migration grants complimentary access to the existing active account whose email is `support@shrijiitservices.com`, using a row named `Subscription.ComplimentaryUser.<user ID>` with value `true`. This grant has no automatic expiry. To grant access temporarily, use an ISO 8601 UTC expiry date instead. Removing the row revokes the grant. The normal daily and monthly scan limits still apply.

The migration never creates a user or grants access during registration based on an unverified email. Verify the grant after deployment. Account deletion removes a complimentary setting belonging to that account.

## Google Play activation

1. Create and activate the monthly subscription and base plan in Play Console, using the IDs above or updating the corresponding database settings. Set the UK consumer price to £1.99 including applicable tax and restrict subscription availability to the UK. The app requires a regular GBP monthly plan and displays the price returned by Google.
2. Give a Google service account the required Play Console permission to view subscriptions and manage orders, and enable the Google Play Android Developer API. Add the service account JSON as the `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` GitHub Actions secret. The Firebase messaging service account's permissions do not automatically include Play billing.
3. Configure authenticated Google Pub/Sub push for real-time developer notifications at `https://fuelmeter.api.shrijiitservices.com/api/subscriptions/google/notifications`. Set `Subscription.Google.NotificationAudience` and `Subscription.Google.NotificationServiceAccount` to match its OIDC audience and sender. Authenticate notification delivery and allow Google's notification publisher to publish to the topic.
4. Configure license testers and install the signed app from a Play test track. Verify purchase, pending payment, acknowledgement, cancellation, restoration, refund, hold and renewal. Google purchase verification uses the signed-in account's protected reference, checks the product, base plan and UK region, and acknowledges eligible purchases from the backend.

The API also reconciles stored subscriptions every five minutes and verifies stale status on demand. A provider outage fails closed for stale paid access. Notification retries and reconciliation read Google's current state rather than granting access directly from a notification.

Keep `SUBSCRIPTION_IDENTITY_KEY` and `SUBSCRIPTION_TOKEN_KEY` as stable 256-bit Base64 GitHub Actions secrets. They protect account/trial identities and encrypt purchase references. Credentials and encryption keys are injected into the published server configuration. Keep secure recovery copies when setting them up. Replacing keys requires a planned data migration.

Google Play purchase tokens remain encrypted in the database while the account exists. Deletion removes account-owned purchase and scan records while retaining a protected trial identity and its dates for one year. An API maintenance process removes expired trial records every five minutes while the API is running. When the API is stopped, cleanup resumes when it next runs.

## AI costs

The configured scan model is `Subscription.Scan.Model`, initially `gpt-5-mini`. The scan ledger records input/output token counts for completed provider responses, including responses that fail to extract a reading. Reference rates are stored in `Subscription.Scan.InputUsdPerMillionTokens` and `Subscription.Scan.OutputUsdPerMillionTokens`, initially $0.25 and $2.00 per million tokens. Provider timeouts can incur charges without returning token usage. Compare the ledger with the provider invoice before adjusting allowances. [OpenAI model pricing](https://developers.openai.com/api/docs/models/gpt-5-mini)

## Verification

Run `dotnet test FuelMeter.Tests/FuelMeter.Tests.csproj` on Windows with SQL Server LocalDB. Tests create and remove separate databases with the prefix `FuelMeter_SubscriptionTests_`. They cover concurrency, credit release, duplicate requests, late completion, account-wide access, encryption and one-year trial retention.
