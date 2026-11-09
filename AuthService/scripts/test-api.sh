#!/usr/bin/env bash
# ==============================================================================
# Full API Test Suite for AuthService (.NET 10 Clean Architecture)
# ==============================================================================

set -euo pipefail

BASE_URL="${AUTH_SERVICE_URL:-http://localhost:5000}"
COOKIE_JAR=$(mktemp)
trap 'rm -f "$COOKIE_JAR"' EXIT

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

PASSED=0
FAILED=0

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_pass() {
    echo -e "${GREEN}[PASS]${NC} $1"
    PASSED=$((PASSED + 1))
}

log_fail() {
    echo -e "${RED}[FAIL]${NC} $1"
    echo -e "${RED}  Response: $2${NC}"
    FAILED=$((FAILED + 1))
}

assert_status() {
    local test_name="$1"
    local expected="$2"
    local actual="$3"
    local response="$4"

    if [ "$actual" -eq "$expected" ]; then
        log_pass "$test_name (HTTP $actual)"
    else
        log_fail "$test_name (Expected $expected, got $actual)" "$response"
    fi
}

echo -e "\n${YELLOW}====================================================================${NC}"
echo -e "${YELLOW}           AuthService Full API Verification Test Suite             ${NC}"
echo -e "${YELLOW}           Target: $BASE_URL                                        ${NC}"
echo -e "${YELLOW}====================================================================${NC}\n"

# ------------------------------------------------------------------------------
# 1. Health Check
# ------------------------------------------------------------------------------
log_info "1. Testing GET /health..."
HTTP_RES=$(curl -s -w "\n%{http_code}" "$BASE_URL/health")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Health check UP" 200 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# 2. Register New User
# ------------------------------------------------------------------------------
RANDOM_SUFFIX=$((RANDOM % 9000 + 1000))
USERNAME="testuser_$RANDOM_SUFFIX"
EMAIL="testuser_$RANDOM_SUFFIX@example.com"
PASSWORD="SecurePassword123!"

log_info "2. Testing POST /api/v1/auth/register ($USERNAME)..."
HTTP_RES=$(curl -s -w "\n%{http_code}" -c "$COOKIE_JAR" -b "$COOKIE_JAR" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$USERNAME\",\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"firstName\":\"John\",\"lastName\":\"Doe\"}" \
    "$BASE_URL/api/v1/auth/register")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Register new user" 200 "$STATUS" "$BODY"

# Check cookies were set
if grep -q "ACCESS_TOKEN" "$COOKIE_JAR" && grep -q "REFRESH_TOKEN" "$COOKIE_JAR"; then
    log_pass "HttpOnly ACCESS_TOKEN & REFRESH_TOKEN cookies written"
else
    log_fail "Cookies missing in CookieJar" "$(cat "$COOKIE_JAR")"
fi

# ------------------------------------------------------------------------------
# 3. Duplicate Registration (Should fail with 409 Conflict)
# ------------------------------------------------------------------------------
log_info "3. Testing POST /api/v1/auth/register with duplicate username..."
HTTP_RES=$(curl -s -w "\n%{http_code}" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$USERNAME\",\"email\":\"other_$RANDOM_SUFFIX@example.com\",\"password\":\"$PASSWORD\",\"firstName\":\"Jane\",\"lastName\":\"Smith\"}" \
    "$BASE_URL/api/v1/auth/register")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Duplicate registration returns 409" 409 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# 4. Check Current Principal GET /api/v1/auth/me
# ------------------------------------------------------------------------------
log_info "4. Testing GET /api/v1/auth/me using authenticated cookie..."
HTTP_RES=$(curl -s -w "\n%{http_code}" -b "$COOKIE_JAR" \
    "$BASE_URL/api/v1/auth/me")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Current user principal (/auth/me)" 200 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# 5. Check User Profile GET /api/v1/users/me
# ------------------------------------------------------------------------------
log_info "5. Testing GET /api/v1/users/me..."
HTTP_RES=$(curl -s -w "\n%{http_code}" -b "$COOKIE_JAR" \
    "$BASE_URL/api/v1/users/me")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "User profile (/users/me)" 200 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# 6. Update Email PATCH /api/v1/users/me/email
# ------------------------------------------------------------------------------
NEW_EMAIL="updated_$RANDOM_SUFFIX@example.com"
log_info "6. Testing PATCH /api/v1/users/me/email..."
HTTP_RES=$(curl -s -w "\n%{http_code}" -b "$COOKIE_JAR" \
    -X PATCH \
    -H "Content-Type: application/json" \
    -d "{\"newEmail\":\"$NEW_EMAIL\"}" \
    "$BASE_URL/api/v1/users/me/email")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Update user email" 200 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# 7. Rotate Refresh Token POST /api/v1/auth/refresh
# ------------------------------------------------------------------------------
log_info "7. Testing POST /api/v1/auth/refresh (Token Rotation)..."
HTTP_RES=$(curl -s -w "\n%{http_code}" -c "$COOKIE_JAR" -b "$COOKIE_JAR" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "{}" \
    "$BASE_URL/api/v1/auth/refresh")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Token rotation (/auth/refresh)" 200 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# 8. Change Password POST /api/v1/auth/change-password
# ------------------------------------------------------------------------------
NEW_PASSWORD="NewPassword456!@"
log_info "8. Testing POST /api/v1/auth/change-password..."
HTTP_RES=$(curl -s -w "\n%{http_code}" -c "$COOKIE_JAR" -b "$COOKIE_JAR" \
    -H "Content-Type: application/json" \
    -d "{\"oldPassword\":\"$PASSWORD\",\"newPassword\":\"$NEW_PASSWORD\"}" \
    "$BASE_URL/api/v1/auth/change-password")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Change password" 204 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# 9. Verify Old Token Invalidation After Password Change
# ------------------------------------------------------------------------------
log_info "9. Verifying old session rejected after password change..."
HTTP_RES=$(curl -s -w "\n%{http_code}" -b "$COOKIE_JAR" \
    "$BASE_URL/api/v1/auth/me")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Old credentials rejected" 401 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# 10. Login With New Password POST /api/v1/auth/login
# ------------------------------------------------------------------------------
log_info "10. Testing POST /api/v1/auth/login with new password..."
HTTP_RES=$(curl -s -w "\n%{http_code}" -c "$COOKIE_JAR" -b "$COOKIE_JAR" \
    -H "Content-Type: application/json" \
    -d "{\"usernameOrEmail\":\"$USERNAME\",\"password\":\"$NEW_PASSWORD\"}" \
    "$BASE_URL/api/v1/auth/login")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Login with new password" 200 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# 11. Logout POST /api/v1/auth/logout
# ------------------------------------------------------------------------------
log_info "11. Testing POST /api/v1/auth/logout (Blacklist JTI + Revoke Refresh)..."
HTTP_RES=$(curl -s -w "\n%{http_code}" -c "$COOKIE_JAR" -b "$COOKIE_JAR" \
    -X POST \
    "$BASE_URL/api/v1/auth/logout")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Logout successfully" 204 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# 12. Verify Session Invalidated After Logout
# ------------------------------------------------------------------------------
log_info "12. Verifying request rejected after logout..."
HTTP_RES=$(curl -s -w "\n%{http_code}" -b "$COOKIE_JAR" \
    "$BASE_URL/api/v1/auth/me")
STATUS=$(echo "$HTTP_RES" | tail -n 1)
BODY=$(echo "$HTTP_RES" | sed '$d')
assert_status "Logged out session rejected" 401 "$STATUS" "$BODY"

# ------------------------------------------------------------------------------
# Summary
# ------------------------------------------------------------------------------
echo -e "\n${YELLOW}====================================================================${NC}"
echo -e "Tests Completed: Passed: ${GREEN}$PASSED${NC} | Failed: ${RED}$FAILED${NC}"
echo -e "${YELLOW}====================================================================${NC}\n"

if [ "$FAILED" -gt 0 ]; then
    exit 1
fi
