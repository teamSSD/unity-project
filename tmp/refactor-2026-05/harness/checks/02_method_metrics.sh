#!/usr/bin/env bash
# 함수 길이 추출 (휴리스틱 — C# 메서드 시그니처 + 중괄호 매칭)
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT="$DATA_DIR/method_lengths.tsv"
: > "$OUT"

extract_method_lengths() {
    file="$1"
    awk -v fname="$file" '
        BEGIN { depth=0; in_method=0; method_start=0; method_name="" }
        {
            line=$0
            if (!in_method && depth==0) {
                if (match(line, /^[ \t]+(public |private |protected |internal |static |async |override |virtual |sealed |new |partial |readonly )+/)) {
                    if (line ~ /\(/ && line !~ /=[^=]/ && line !~ /class +/ && line !~ /struct +/ && line !~ /interface +/ && line !~ /enum +/ && line !~ / event /) {
                        sig=line
                        gsub(/<[^>]*>/, "", sig)
                        if (match(sig, /[A-Za-z_][A-Za-z0-9_]*[ \t]*\(/)) {
                            method_name=substr(sig, RSTART, RLENGTH-1)
                            gsub(/[ \t]/, "", method_name)
                            method_start=NR
                            in_method=1
                            if (line ~ /\{[ \t]*$/) { depth=1 }
                            else if (line ~ /=>/) {
                                printf "%d\t%s\t%s\n", 1, method_name, fname
                                in_method=0
                            }
                        }
                    }
                }
            } else if (in_method) {
                if (depth==0 && line ~ /\{/) depth=1
                else {
                    n=gsub(/\{/, "&", line); depth+=n
                    n=gsub(/\}/, "&", line); depth-=n
                    if (depth<=0) {
                        printf "%d\t%s\t%s\n", NR-method_start+1, method_name, fname
                        in_method=0
                        depth=0
                    }
                }
            }
        }
    ' "$file"
}

while IFS= read -r f; do
    [ -z "$f" ] && continue
    extract_method_lengths "$f" >> "$OUT"
done < "$RUNTIME_LIST"

echo "method_metrics: $(wc -l < "$OUT" | tr -d ' ') methods" >&2
