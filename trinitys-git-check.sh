#!/data/data/com.termux/files/usr/bin/bash
# Script corporativo para verificar estado del repositorio TrinityS-Ark

echo "=== Rama activa ==="
git branch

echo "=== Último commit ==="
git log --oneline -1

echo "=== Remotos configurados ==="
git remote -v

echo "=== Estado del repositorio ==="
git status

echo "=== Archivos descargados ==="
ls -la

