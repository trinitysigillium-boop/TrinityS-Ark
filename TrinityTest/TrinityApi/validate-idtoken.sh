#!/bin/bash

# Endpoint JWKS de Web3Auth
JWKS_URL="https://api-auth.web3auth.io/.well-known/jwks.json"

# Token a validar (pásalo como argumento)
ID_TOKEN=$1

if [ -z "$ID_TOKEN" ]; then
  echo "Uso: ./validate-idtoken.sh <id_token>"
  exit 1
fi

# Descargar JWKS
echo "[INFO] Descargando JWKS desde $JWKS_URL..."
JWKS=$(curl -s $JWKS_URL)

# Guardar JWKS en archivo temporal
echo "$JWKS" > /tmp/jwks.json

# Validar token con jwt-cli (instalar con npm install -g jwt-cli)
echo "[INFO] Validando token..."
jwt decode "$ID_TOKEN" --jwks /tmp/jwks.json

