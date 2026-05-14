#!/data/data/com.termux/files/usr/bin/bash
# Script corporativo para reparar conexión SSH con GitHub

echo "=== Verificando claves SSH en ~/.ssh ==="
ls -la ~/.ssh/

echo "=== Iniciando agente SSH ==="
eval "$(ssh-agent -s)"

echo "=== Agregando clave privada al agente ==="
ssh-add ~/.ssh/id_rsa

echo "=== Probando conexión con GitHub ==="
ssh -T git@github.com

echo "=== Intentando push al remoto ==="
cd ~/TrinityS-Ark
git push origin main

echo "=== Flujo SSH corporativo completado ==="

