#!/data/data/com.termux/files/usr/bin/bash
# Reporte corporativo de repositorios TrinityS

# Carpeta base donde guardas tus proyectos
BASE_DIR=~/TrinityS-Ark
FORKS_DIR=~/Forks

echo "===== Reporte de Repositorios (TrinityS) ====="
echo "Fecha: $(date)"
echo "Usuario: $(whoami)"
echo "=============================================="

# Función para revisar repositorios
check_repo () {
  cd "$1" || return
  echo ">> Repositorio: $(basename $1)"
  echo "   Rama activa: $(git rev-parse --abbrev-ref HEAD)"
  echo "   Estado:"
  git status -s
  echo "   Últimos commits:"
  git log --oneline -5
  echo "----------------------------------------------"
}

# Revisar proyecto principal
check_repo "$BASE_DIR"

# Revisar forks (si existen)
for repo in $FORKS_DIR/*; do
  if [ -d "$repo/.git" ]; then
    check_repo "$repo"
  fi
done

