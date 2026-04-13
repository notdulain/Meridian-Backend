# Meridian User Manual

## Overview

Meridian is a logistics operations platform for managing deliveries, drivers, vehicles, assignments, route planning, live tracking, and operational reporting.

This manual is organized by user role:

- **Admin:** manages drivers, vehicles, and operational records.
- **Dispatcher:** creates deliveries, assigns drivers and vehicles, plans routes, and monitors live tracking.
- **Driver:** views active assignments and shares live location updates.
- **Manager:** reviews dashboards, trends, reports, and CSV exports.

> Screenshot placeholder: `docs/screenshots/user-manual/01-login-page.png`

---

## 1. Logging In

1. Open the Meridian web application.
2. Enter your email and password.
3. Select **Login**.
4. The system redirects you to the dashboard for your role.

If login fails, confirm that the account is active and the credentials are correct.

> Screenshot placeholder: `docs/screenshots/user-manual/02-login-error.png`

---

## 2. Admin Workflow

### 2.1 View Admin Dashboard

1. Log in as an Admin.
2. Open the Admin dashboard.
3. Review delivery and operations summary cards.

> Screenshot placeholder: `docs/screenshots/user-manual/03-admin-dashboard.png`

### 2.2 Manage Drivers

1. Open **Drivers**.
2. Select the create/add driver action.
3. Enter the driver details:
   - full name
   - email
   - password
   - license number
   - license expiry date
   - phone number
   - maximum working hours per day
4. Submit the form.
5. Confirm the driver appears in the driver list.

> Screenshot placeholder: `docs/screenshots/user-manual/04-drivers-list.png`
>
> Screenshot placeholder: `docs/screenshots/user-manual/05-create-driver-form.png`

### 2.3 Manage Vehicles

1. Open **Vehicles**.
2. Select the create/add vehicle action.
3. Enter the vehicle details:
   - plate number
   - make
   - model
   - current location
   - year
   - capacity in kilograms
   - capacity in cubic meters
   - fuel efficiency
   - status
4. Submit the form.
5. Confirm the vehicle appears in the vehicle list.

> Screenshot placeholder: `docs/screenshots/user-manual/06-vehicles-list.png`
>
> Screenshot placeholder: `docs/screenshots/user-manual/07-create-vehicle-form.png`

---

## 3. Dispatcher Workflow

### 3.1 View Dispatcher Dashboard

1. Log in as a Dispatcher.
2. Open the Dispatcher dashboard.
3. Review active deliveries, completed deliveries, pending deliveries, and overdue deliveries.

> Screenshot placeholder: `docs/screenshots/user-manual/08-dispatcher-dashboard.png`

### 3.2 Create a Delivery

1. Open **Deliveries**.
2. Select **Create Delivery**.
3. Enter pickup address, delivery address, package weight, package volume, deadline, and creator name.
4. Submit the delivery.
5. Confirm the delivery appears in the delivery list.

> Screenshot placeholder: `docs/screenshots/user-manual/09-deliveries-list.png`
>
> Screenshot placeholder: `docs/screenshots/user-manual/10-create-delivery-form.png`

### 3.3 View Delivery Details

1. Open **Deliveries**.
2. Select a delivery.
3. Review addresses, package details, deadline, current status, assignment information, and status history.

> Screenshot placeholder: `docs/screenshots/user-manual/11-delivery-details.png`

### 3.4 Use Vehicle Recommendations

1. Open a delivery details page.
2. Use the vehicle recommendation action.
3. Review suggested vehicles based on capacity and availability.
4. Use a suitable recommended vehicle when creating an assignment.

> Screenshot placeholder: `docs/screenshots/user-manual/12-vehicle-recommendations.png`

### 3.5 Create an Assignment

1. Open **Assignments**.
2. Select the create assignment action.
3. Choose a delivery.
4. Choose an available vehicle.
5. Choose an available driver.
6. Add notes if needed.
7. Submit the assignment.
8. Confirm the assignment appears in the assignment list.

> Screenshot placeholder: `docs/screenshots/user-manual/13-assignments-list.png`
>
> Screenshot placeholder: `docs/screenshots/user-manual/14-create-assignment.png`

### 3.6 Complete or Cancel an Assignment

1. Open **Assignments**.
2. Select an assignment.
3. Select **Complete** when delivery work is finished.
4. Select **Cancel** when the assignment should stop.
5. Confirm the status update.

> Screenshot placeholder: `docs/screenshots/user-manual/15-assignment-status-actions.png`

### 3.7 Monitor Live Tracking

1. Open **Tracking**.
2. Review active delivery markers on the map.
3. Select markers to inspect live driver/delivery location details.
4. Watch marker positions update when drivers send GPS updates.

> Screenshot placeholder: `docs/screenshots/user-manual/19-dispatcher-live-tracking.png`

---

## 4. Route Planning

### 4.1 Calculate and Compare Routes

1. Open **Routes**.
2. Enter origin and destination.
3. Calculate the route.
4. Review the map, distance, duration, estimated fuel cost, and route options.
5. Select the best route when required.

> Screenshot placeholder: `docs/screenshots/user-manual/16-route-calculation.png`
>
> Screenshot placeholder: `docs/screenshots/user-manual/17-route-comparison.png`

### 4.2 View Fuel Cost Report

1. Open **Routes**.
2. Use the fuel cost report section.
3. Select a vehicle or choose **All Vehicles**.
4. Set the date range.
5. Review trip count, distance, fuel consumption, and fuel cost.
6. Export the report as CSV if required.

> Screenshot placeholder: `docs/screenshots/user-manual/18-fuel-cost-report.png`

---

## 5. Driver Workflow

### 5.1 View Active Assignment

1. Log in as a Driver.
2. Open the Driver dashboard.
3. Review the active assignment card.
4. Check the delivery ID, assignment status, GPS tracking status, and assigned vehicle.

> Screenshot placeholder: `docs/screenshots/user-manual/20-driver-dashboard-active-assignment.png`

### 5.2 Share Live Location Updates

1. Log in as a Driver.
2. Open the Driver dashboard.
3. Review the active assignment.
4. Allow browser location permission.
5. Keep the dashboard open during active delivery work so the system can send location updates.

> Screenshot placeholder: `docs/screenshots/user-manual/21-location-permission-prompt.png`
>
> Screenshot placeholder: `docs/screenshots/user-manual/22-driver-live-location.png`

---

## 6. Manager Reports

### 6.1 View Dashboard and Delivery Trends

1. Open the manager dashboard or reporting area.
2. Review operational summary metrics.
3. Open **Delivery Trends**.
4. Review delivery volume and status trends over time.

> Screenshot placeholder: `docs/screenshots/user-manual/23-manager-dashboard.png`
>
> Screenshot placeholder: `docs/screenshots/user-manual/24-delivery-trends.png`

### 6.2 View Driver Performance

1. Open the Driver Performance report.
2. Select start and end dates.
3. Review completed deliveries, average delivery time, and on-time rate.
4. Export CSV if required.

> Screenshot placeholder: `docs/screenshots/user-manual/25-driver-performance-report.png`

### 6.3 View Vehicle Utilization

1. Open the Vehicle Utilization report.
2. Select start and end dates.
3. Review trip count, distance, active time, and utilization rate.
4. Export CSV if required.

> Screenshot placeholder: `docs/screenshots/user-manual/26-vehicle-utilization-report.png`

### 6.4 View Delivery Success Rate

1. Open the Delivery Success Rate report.
2. Select start and end dates.
3. Review delivered, failed, cancelled, terminal delivery count, and success rate.
4. Export CSV if required.

> Screenshot placeholder: `docs/screenshots/user-manual/27-delivery-success-rate-report.png`
>
> Screenshot placeholder: `docs/screenshots/user-manual/28-csv-export-button.png`

---

## 7. Profile and Settings

1. Open **Profile** to review account details.
2. Open **Settings** to review available preferences.

> Screenshot placeholder: `docs/screenshots/user-manual/29-profile-page.png`
>
> Screenshot placeholder: `docs/screenshots/user-manual/30-settings-page.png`

---

## 8. Common Issues

| Issue | Meaning | Action |
| --- | --- | --- |
| Login failed | Invalid credentials or inactive account | Check credentials or contact an Admin |
| Unauthorized / 401 | Session missing or expired | Log in again |
| Forbidden / 403 | Role cannot perform the action | Ask an Admin to confirm permissions |
| No active assignment | Driver has no current assignment | Contact the dispatcher |
| No vehicle recommendations | No matching available vehicle | Check vehicle availability and capacity |
| Report has no data | Filters return no matching records | Change the date range or filters |
| Location permission denied | Browser blocked GPS access | Allow location permission and reload |
| Service unavailable | Backend or gateway issue | Retry and report if it continues |

> Screenshot placeholder: `docs/screenshots/user-manual/31-report-empty-state.png`
>
> Screenshot placeholder: `docs/screenshots/user-manual/32-error-banner.png`

---

## 9. Screenshot Checklist

Capture these screenshots and save them under `docs/screenshots/user-manual/`.

| # | Filename | Screen |
| --- | --- | --- |
| 01 | `01-login-page.png` | Login page |
| 02 | `02-login-error.png` | Login error state |
| 03 | `03-admin-dashboard.png` | Admin dashboard |
| 04 | `04-drivers-list.png` | Drivers list |
| 05 | `05-create-driver-form.png` | Create driver form |
| 06 | `06-vehicles-list.png` | Vehicles list |
| 07 | `07-create-vehicle-form.png` | Create vehicle form |
| 08 | `08-dispatcher-dashboard.png` | Dispatcher dashboard |
| 09 | `09-deliveries-list.png` | Deliveries list |
| 10 | `10-create-delivery-form.png` | Create delivery form |
| 11 | `11-delivery-details.png` | Delivery details page |
| 12 | `12-vehicle-recommendations.png` | Vehicle recommendations panel |
| 13 | `13-assignments-list.png` | Assignments list |
| 14 | `14-create-assignment.png` | Create assignment flow |
| 15 | `15-assignment-status-actions.png` | Assignment actions |
| 16 | `16-route-calculation.png` | Route calculation |
| 17 | `17-route-comparison.png` | Route comparison |
| 18 | `18-fuel-cost-report.png` | Fuel cost report |
| 19 | `19-dispatcher-live-tracking.png` | Dispatcher tracking map |
| 20 | `20-driver-dashboard-active-assignment.png` | Driver active assignment |
| 21 | `21-location-permission-prompt.png` | Location permission prompt |
| 22 | `22-driver-live-location.png` | Driver live location |
| 23 | `23-manager-dashboard.png` | Manager dashboard |
| 24 | `24-delivery-trends.png` | Delivery trends |
| 25 | `25-driver-performance-report.png` | Driver performance report |
| 26 | `26-vehicle-utilization-report.png` | Vehicle utilization report |
| 27 | `27-delivery-success-rate-report.png` | Delivery success report |
| 28 | `28-csv-export-button.png` | CSV export button |
| 29 | `29-profile-page.png` | Profile page |
| 30 | `30-settings-page.png` | Settings page |
| 31 | `31-report-empty-state.png` | Empty report state |
| 32 | `32-error-banner.png` | Error banner |
