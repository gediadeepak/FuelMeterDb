# Troubleshooting 403 ModSecurity Error

## Problem
When running the FuelMeter.Web application, you're getting:
```
Response status code does not indicate success: 403 (ModSecurity Action).
```

## What is ModSecurity?
ModSecurity is a Web Application Firewall (WAF) that protects your API server from malicious requests. It's blocking legitimate requests from your web app because they match certain security patterns.

## Solutions

### Solution 1: Add Proper Headers (Already Implemented)
✅ **Done** - Added User-Agent and Accept headers to all HTTP requests.

```csharp
client.DefaultRequestHeaders.Add("User-Agent", "FuelMeterWeb/1.0");
client.DefaultRequestHeaders.Add("Accept", "application/json");
```

### Solution 2: Whitelist Your IP Address
Contact your hosting provider (shrijiitservices.com) and ask them to:
1. Whitelist your development machine's IP address
2. Whitelist the IP address of your production web server
3. Add your application's User-Agent to the WAF allowlist

### Solution 3: Adjust ModSecurity Rules
If you have access to the server configuration, you can:

1. **Check ModSecurity Logs** to see which rule is triggering:
```bash
# On your API server
tail -f /var/log/modsec_audit.log
```

2. **Whitelist Specific Rules** by adding to your Apache/Nginx config:
```apache
# Disable specific rule IDs for your API endpoints
<LocationMatch "^/api/">
	SecRuleRemoveById 950001
	SecRuleRemoveById 950109
</LocationMatch>
```

3. **Whitelist by User-Agent**:
```apache
SecRule REQUEST_HEADERS:User-Agent "FuelMeterWeb" "id:1000,phase:1,allow,ctl:ruleEngine=Off"
```

### Solution 4: Use CORS and Referer Headers
Add these to your API's CORS configuration:

In `FuelMeter.Api/Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowWebApp", policy =>
	{
		policy.WithOrigins(
			"https://localhost:5001",
			"https://your-web-app-domain.com"
		)
		.AllowAnyHeader()
		.AllowAnyMethod()
		.AllowCredentials();
	});
});

// After var app = builder.Build();
app.UseCors("AllowWebApp");
```

### Solution 5: Test with Development API
For local development, you can:

1. **Run API locally** without ModSecurity:
```bash
cd FuelMeter.Api
dotnet run
```

2. **Update appsettings.Development.json**:
```json
{
  "ApiSettings": {
	"BaseUrl": "https://localhost:7001/"
  }
}
```

3. **Keep production URL** in `appsettings.json` for deployment.

### Solution 6: Content-Type for POST Requests
Some WAFs block JSON with specific patterns. Ensure all POST requests use proper Content-Type.

Check `ApiClientService.cs` (should already have this):
```csharp
var content = new StringContent(
	JsonSerializer.Serialize(data),
	Encoding.UTF8,
	"application/json"
);
```

## Testing the Fix

### Test 1: Simple GET Request
Try accessing the API directly:
```bash
curl -H "User-Agent: FuelMeterWeb/1.0" \
	 -H "Accept: application/json" \
	 https://fuelmeter.api.shrijiitservices.com/api/budget
```

If this works, the issue is with authentication or specific endpoints.

### Test 2: Check Specific Endpoint
Test the endpoint that's failing:
1. Open browser DevTools (F12)
2. Go to Network tab
3. Reproduce the error
4. Check which request returned 403
5. Look at Request Headers and Request Payload

### Test 3: Check Authentication
Make sure JWT token is being sent:
```csharp
// In ApiClientService.cs
var request = new HttpRequestMessage(HttpMethod.Get, url);
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
```

## Most Likely Causes

Based on your setup, the most likely issues are:

1. ❌ **Missing Authentication Token** - Are you logged in?
2. ❌ **WAF blocking User-Agent** - Fixed by adding User-Agent header
3. ❌ **IP-based blocking** - Your IP needs to be whitelisted
4. ❌ **Request payload patterns** - Some data in requests triggers WAF

## Recommended Actions

### Immediate Steps:
1. ✅ User-Agent and Accept headers added
2. ✅ HTTP services registered
3. ⚠️ **Contact hosting provider** to whitelist:
   - Your IP address
   - User-Agent: `FuelMeterWeb/1.0`
   - All `/api/*` endpoints for your domain

### Alternative: Development Mode
For immediate testing, switch to local API:

```json
// appsettings.Development.json
{
  "ApiSettings": {
	"BaseUrl": "https://localhost:7001/"  // Local API
  }
}
```

Then run both:
```bash
# Terminal 1
cd FuelMeter.Api
dotnet run

# Terminal 2
cd FuelMeter.Web
dotnet run
```

## Contact Hosting Provider

Email your hosting provider with:

```
Subject: Whitelist Request for FuelMeter Application

Hello,

I'm experiencing 403 ModSecurity errors when my Blazor web application 
tries to access my API at fuelmeter.api.shrijiitservices.com.

Please whitelist the following:

1. User-Agent: FuelMeterWeb/1.0
2. Client IP: [Your IP Address]
3. Endpoints: All /api/* routes
4. Methods: GET, POST, PUT, DELETE
5. Content-Type: application/json

This is a legitimate application making REST API calls with JWT authentication.

Thank you!
```

## Still Having Issues?

If the problem persists after these fixes:
1. Share the exact endpoint that's failing
2. Check the browser console for detailed error messages
3. Verify authentication token is valid
4. Test with a tool like Postman to isolate the issue
