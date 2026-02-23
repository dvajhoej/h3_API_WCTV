#!/usr/bin/env bash

ROOT="$(cd "$(dirname "$0")" && pwd)"
LOG="$ROOT/api-startup.log"

echo ""
echo " [1/2] Starting WCTV.Api  (output → api-startup.log)"
cd "$ROOT/WCTV.Api"
dotnet run > "$LOG" 2>&1 &
API_PID=$!

echo -n " [2/2] Waiting for API on localhost:5000 "
TRIES=0
until curl -sf "http://localhost:5000/api/kpi" > /dev/null 2>&1; do
    # Check if the dotnet process died on its own
    if ! kill -0 "$API_PID" 2>/dev/null; then
        echo ""
        echo " ERROR: dotnet process exited unexpectedly."
        echo " ── api-startup.log ──────────────────────────"
        cat "$LOG"
        echo " ─────────────────────────────────────────────"
        exit 1
    fi

    TRIES=$((TRIES + 1))
    if [ $TRIES -ge 45 ]; then
        echo ""
        echo " ERROR: API did not respond after 90 seconds."
        echo " ── api-startup.log (last 40 lines) ──────────"
        tail -40 "$LOG"
        echo " ─────────────────────────────────────────────"
        kill "$API_PID" 2>/dev/null
        exit 1
    fi

    printf "."
    sleep 2
done
echo " ready!"
rm -f "$LOG"

echo " [3/3] Starting wctv-dashboard ..."
cd "$ROOT/wctv-dashboard"
npm run dev &
FRONT_PID=$!

echo ""
echo "  API:       http://localhost:5000"
echo "  Dashboard: http://localhost:5173"
echo "  Swagger:   http://localhost:5000/swagger"
echo ""
echo "  Press Ctrl+C to stop both."
echo ""

trap 'echo ""; echo " Stopping..."; kill "$API_PID" "$FRONT_PID" 2>/dev/null; exit 0' INT TERM
wait
