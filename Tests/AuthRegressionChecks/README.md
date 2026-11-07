# Authentication regression checks

Run from the repository root:

```powershell
dotnet run --project Tests/AuthRegressionChecks -- .
dotnet run --project Tests/ExamSecurityChecks -- .
```

AuthRegressionChecks creates a uniquely named QLThiTN_AuthRegression_* SQL database using the configured server. It deliberately takes only that database offline, verifies API/WinForms denial and recovery, then deletes it in finally. It never takes the configured application database offline. Run only with a SQL account allowed to create/drop test databases. Logs contain no credentials or tokens.

The 19 checks cover email-only Google impersonation and malformed tokens over HTTP, no unwanted account creation, immediate rejection after account lock/password change, role changes, and denial/recovery during a real SQL database outage. Successful Google OAuth login with a real user has not been tested; neither have all 96 thesis cases or real payments. These checks do not establish rollback correctness for every business transaction.

Google login now requires an ID token validated by Google.Apis.Auth (signature, issuer, expiry and audience), plus verified email. Email/name are taken from the verified payload. The website forwards the ID token received from Google; the simulated login route has been removed.

Set Authentication:Google:ClientId in API appsettings.Google.json to the same client ID as the Web application. Copy the API template for a new environment. The Web retains its own client secret. Local Google config files are ignored by git; API Google config is excluded from publishing. No client secret is required in the API.

API permission checks now query persisted account state on each authenticated request. This adds a small SQL read per request and avoids the previous 60-second authorization cache. WinForms checks permissions synchronously with five-second connection/command timeouts; a SQL failure stops the operation with a connection-specific error and permits retry after recovery. It does not authorize based on a cached active flag.

The older audit fixtures were updated for current owner-account fields and free/paid exam-session settings. EF-generated parent-status defaults are removed only inside disposable test databases to accommodate the existing schema initializer. That production initializer issue has not been changed here.

References:
- https://developers.google.com/identity/openid-connect/openid-connect
- https://developers.google.com/identity/sign-in/web/backend-auth

Evidence: audit/2026-11-06-auth/auth-regression.log
