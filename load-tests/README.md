# k6 load tests

## Run

From the repository root (or `load-tests/`):

```bash
export BASE_URL="https://<your-gateway>.azurecontainerapps.io"
export K6_LOGIN_EMAIL="<qa user email>"
export K6_LOGIN_PASSWORD="<secret>"

# Optional — real IDs from QA database
export DELIVERY_ID=1
export VEHICLE_ID=1
export DRIVER_ID=1

k6 run load-tests/dispatcher-session.js
```

Do **not** commit passwords. Copy `.env.example` to `.env` locally if you prefer; `.env` is gitignored.

## QA gateway paths

This script uses the deployed gateway routes: `/api/auth/login`, `/delivery/api/deliveries`, `/assignment/api/assignments`.