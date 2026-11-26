#!/bin/bash
TOKEN=$(cat /d/00_BARANG/.ironing-build/admin-token.txt)
curl -s -H "Authorization: Bearer $TOKEN" -H "X-Company-ID: 42f0e459-6095-45bb-aa13-f38126d91d9c" -H "Content-Type: application/json" "$@"
