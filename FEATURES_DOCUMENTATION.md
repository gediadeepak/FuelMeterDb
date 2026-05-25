# FuelMeter - New Features Documentation

## Overview
This document describes the three major features added to FuelMeter: Export/Import, Bill Estimation, and Budget Tracking.

---

## 1. Export/Import Functionality

### Features
- **Export Data**: Export all your meter readings, budgets, and settings to JSON or CSV format
- **Import Data**: Restore data from previously exported files
- **Duplicate Detection**: Automatically skips duplicate meter readings
- **Validation**: Validates data before import and provides detailed error messages

### API Endpoints

#### Export
- `GET /api/ExportImport/export/json` - Get export data as JSON object
- `GET /api/ExportImport/download/json` - Download JSON file
- `GET /api/ExportImport/download/csv` - Download CSV file

#### Import
- `POST /api/ExportImport/import/json` - Import from JSON string
- `POST /api/ExportImport/import/csv` - Import from CSV string
- `POST /api/ExportImport/upload/json` - Upload JSON file
- `POST /api/ExportImport/upload/csv` - Upload CSV file
- `POST /api/ExportImport/validate` - Validate import data without saving

### Usage

#### Web/MAUI Application
Navigate to **Export/Import** from the navigation menu:
- Click "Export as JSON" or "Export as CSV" to download your data
- Choose a previously exported file and click "Import Data"
- Review the import results showing items imported and any warnings/errors

#### Code Example (API Client)
```csharp
// Export to JSON
var json = await httpClient.GetStringAsync("/api/ExportImport/export/json");

// Import from file
var content = File.ReadAllText("fuelmeter-data.json");
var result = await httpClient.PostAsJsonAsync("/api/ExportImport/import/json", content);
```

---

## 2. Bill Estimation

### Features
- **Custom Period Estimation**: Calculate bills for any date range
- **Current Month Estimation**: Get real-time bill estimate for the current month
- **Bill History**: View bill history for up to 12 months
- **Month-over-Month Comparison**: Compare current month's bill with previous month
- **Projections**: Project end-of-month bill based on current usage
- **Daily Average Cost**: Calculate daily average spending

### API Endpoints

#### Bill Estimation
- `POST /api/BillEstimation/estimate` - Estimate bill for custom date range
- `GET /api/BillEstimation/current/{fuelType}` - Estimate current month bill
- `GET /api/BillEstimation/summaries/{year}` - Get monthly summaries for a year
- `GET /api/BillEstimation/history/{fuelType}?months=12` - Get bill history

#### Comparisons & Projections
- `GET /api/BillEstimation/compare/{fuelType}/{year}/{month}` - Compare with previous month
- `GET /api/BillEstimation/project/{fuelType}/{year}/{month}` - Project monthly bill
- `GET /api/BillEstimation/daily-average/{fuelType}?startDate&endDate` - Calculate daily average
- `GET /api/BillEstimation/dashboard` - Get comprehensive dashboard data

### Usage

#### Dashboard Widgets
The dashboard now displays:
- **Bill Comparison Cards**: Shows current vs. previous month with trend indicators (↑↓→)
- Visual indicators for cost increases (red) and decreases (green)

#### Code Example
```csharp
// Estimate current month bill
var estimation = await billService.EstimateCurrentMonthBillAsync(userId, "Electricity");
Console.WriteLine($"Estimated Bill: £{estimation.TotalEstimatedBill}");
Console.WriteLine($"Units Consumed: {estimation.UnitsConsumed} kWh");
Console.WriteLine($"Daily Average: £{estimation.DailyAverageCost}");

// Compare with previous month
var comparison = await billService.CompareBillsAsync(userId, "Gas", 2025, 5);
Console.WriteLine($"Change: {comparison.Trend} by £{Math.Abs(comparison.ChangeAmount)}");
```

### Calculation Formula
```
Total Bill = (Units Consumed × Unit Rate) + (Standing Charge × Days in Period)

Where:
- Units Consumed = End Reading - Start Reading
- Unit Rate = From user settings (pence per kWh or unit)
- Standing Charge = From user settings (pence per day)
```

---

## 3. Budget Tracking

### Features
- **Monthly Budgets**: Set monthly spending limits for Electricity and Gas
- **Real-time Tracking**: See actual spend vs. budget with progress bars
- **Over-Budget Alerts**: Visual warnings when spending exceeds budget
- **Year Navigation**: Browse budgets by year
- **Budget CRUD**: Create, edit, update, and delete budgets

### API Endpoints

#### Budget Management
- `GET /api/Budget` - Get all budgets for user
- `GET /api/Budget/year/{year}` - Get budgets for specific year
- `GET /api/Budget/{id}` - Get single budget by ID
- `GET /api/Budget/month/{fuelType}/{year}/{month}` - Get budget for specific month
- `POST /api/Budget` - Create new budget
- `PUT /api/Budget/{id}` - Update existing budget
- `DELETE /api/Budget/{id}` - Delete budget

#### Budget Tracking
- `GET /api/Budget/summary/{fuelType}/{year}/{month}` - Get budget summary with actual spend
- `GET /api/Budget/status/{year}` - Get monthly status for all months in a year
- `GET /api/Budget/alerts` - Get all over-budget alerts

### Usage

#### Budget Management Page
Navigate to **Budget Management** from the navigation menu:
- Click "+ New Budget" to create a budget
- View budget cards showing:
  - Budget limit
  - Actual spend
  - Remaining budget
  - Progress bar with percentage used
- Over-budget items are highlighted in red
- Edit or delete existing budgets

#### Dashboard Integration
The dashboard displays:
- **Budget Summary Cards**: Mini cards showing budget status for current month
- Progress bars with color coding (green = on track, red = over budget)
- Quick link to manage budgets

#### Code Example
```csharp
// Create a budget
var createDto = new CreateBudgetDto("Electricity", 2025, 5, 120.00m, "May budget");
var budget = await budgetService.CreateBudgetAsync(userId, createDto);

// Get budget summary
var summary = await budgetService.GetBudgetSummaryAsync(userId, "Electricity", 2025, 5);
Console.WriteLine($"Budget: £{summary.BudgetLimit}");
Console.WriteLine($"Spent: £{summary.ActualSpent}");
Console.WriteLine($"Remaining: £{summary.RemainingBudget}");
Console.WriteLine($"Usage: {summary.PercentageUsed}%");
Console.WriteLine($"Over Budget: {summary.IsOverBudget}");

// Get over-budget alerts
var alerts = await budgetService.GetOverBudgetAlertsAsync(userId);
foreach (var alert in alerts)
{
	Console.WriteLine($"{alert.FuelType} {alert.Month}/{alert.Year}: Over by £{Math.Abs(alert.RemainingBudget)}");
}
```

---

## Database Schema

### Budgets Table
```sql
CREATE TABLE Budgets (
	Id INT PRIMARY KEY IDENTITY,
	UserId INT NOT NULL,
	FuelType NVARCHAR(50) NOT NULL,
	Year INT NOT NULL,
	Month INT NOT NULL,
	BudgetLimit DECIMAL(18,2) NOT NULL,
	Notes NVARCHAR(500),
	CreatedAt DATETIME2 NOT NULL,
	UpdatedAt DATETIME2,
	CONSTRAINT FK_Budgets_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_Budget_User_FuelType_Year_Month 
	ON Budgets(UserId, FuelType, Year, Month);
```

---

## Integration Across Platforms

### Android (MAUI App)
All features are accessible through:
1. **Navigation Menu**: Updated with links to Budget Management and Export/Import
2. **Dashboard**: Displays budget widgets and bill comparisons
3. **API Integration**: All features call the same REST API endpoints

### Web Application (Blazor Server)
Identical UI and functionality as MAUI app:
1. **Shared Components**: Uses FuelMeter.Components project
2. **Real-time Updates**: Budget tracking updates automatically
3. **Responsive Design**: Mobile-friendly layouts

### Web API
All features exposed through REST API:
1. **JWT Authentication**: All endpoints require authentication
2. **OpenAPI Documentation**: Endpoints documented with Swagger
3. **Error Handling**: Comprehensive error responses

---

## Security Considerations

1. **Authentication**: All endpoints require JWT bearer token
2. **Authorization**: Users can only access their own data
3. **Data Validation**: Input validation on all DTOs
4. **SQL Injection Protection**: Entity Framework parameterized queries
5. **File Upload Limits**: 10MB max for import files

---

## Testing

### Manual Testing Checklist

#### Export/Import
- [ ] Export data as JSON
- [ ] Export data as CSV
- [ ] Import JSON file successfully
- [ ] Import CSV file successfully
- [ ] Verify duplicate detection works
- [ ] Test validation with invalid data

#### Bill Estimation
- [ ] View current month bill estimate
- [ ] Compare bills month-over-month
- [ ] View bill history chart
- [ ] Verify calculations are accurate
- [ ] Test with custom date ranges

#### Budget Tracking
- [ ] Create monthly budgets
- [ ] Edit existing budgets
- [ ] Delete budgets
- [ ] Verify over-budget alerts appear
- [ ] Check dashboard budget widgets
- [ ] Test year navigation

### Unit Testing
Key service methods to test:
- `BudgetService.CalculateActualSpentAsync()`
- `BillEstimationService.EstimateBillAsync()`
- `ExportImportService.ValidateImportDataAsync()`

---

## Troubleshooting

### Import Fails
- Check file format is valid JSON or CSV
- Ensure FuelType values are "Electricity" or "Gas"
- Verify dates are in valid format
- Check for negative values in readings or budgets

### Budget Not Updating
- Ensure meter readings exist for the month
- Verify user settings have unit rates configured
- Check that at least 2 readings exist for the month

### Bill Estimation Shows Zero
- Add at least 2 meter readings for the period
- Configure unit rates in Settings
- Ensure readings are in chronological order

---

## Future Enhancements

Potential improvements:
1. **Scheduled Exports**: Automatic weekly/monthly exports
2. **Email Alerts**: Send notifications when over budget
3. **Budget Templates**: Copy budgets from previous months
4. **Multi-year Comparison**: Compare bills across years
5. **Export to Excel**: Enhanced Excel export with charts
6. **Budget Categories**: Sub-categories within fuel types
7. **Predictive Analytics**: ML-based bill predictions

---

## Support

For issues or questions:
1. Check the error messages in import results
2. Review API endpoint documentation (Swagger UI)
3. Verify authentication token is valid
4. Check database migrations are applied

---

**Last Updated**: January 2025
**Version**: 1.0
**Authors**: Development Team
