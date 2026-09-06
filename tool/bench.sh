#!/bin/bash
# Runs one example and reports the median GPU frame cost over a settled window.
#
#   tool/bench.sh "A hundred thousand" 30
#
# Reports what the frame cost the GPU, taken from Filament's own frame history
# rather than from how often anything was presented — presentation is the
# display's business and no amount of headroom shows up there.
# Discards the first few readings: the first frames of any run include shader
# compilation, texture uploads and a cold cache, and reporting those as the
# cost of a frame is how a measurement lies.
EX="$1"; SECS="${2:-30}"
cd ~/development/projects/Personal/orbis-examples/gallery
rm -f /tmp/bench.log
ORBIS_PACE=1 ORBIS_EXAMPLE="$EX" ./build/macos/Build/Products/Debug/orbis_gallery.app/Contents/MacOS/orbis_gallery > /tmp/bench.log 2>&1 &
sleep 4
osascript -e 'tell application "System Events" to set frontmost of first process whose name contains "orbis_gallery" to true' 2>/dev/null
sleep "$SECS"
pkill -f orbis_gallery; sleep 1
grep -o "gpu [0-9.]* ms" /tmp/bench.log | awk '{print $2}' | tail -n +4 | sort -n | awk '
  { v[NR]=$1 }
  END {
    if (NR==0) { print "no readings"; exit }
    printf "  %-22s median %.2f ms  (%.0f fps)   min %.2f  max %.2f  n=%d\n",
           ENVIRON["LABEL"], v[int((NR+1)/2)], 1000/v[int((NR+1)/2)], v[1], v[NR], NR
  }'
