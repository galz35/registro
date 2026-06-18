#!/bin/bash
# Script de deploy para Asistencia
# Uso: bash deploy/deploy.sh

set -e

echo "=== Deploy Asistencia ==="

# 1. Backend
echo "[1/4] Compilando backend..."
cd /opt/apps/asistencia/registro/nest
npm run build

echo "[2/4] Instalando dependencias de produccion..."
npm install --production

echo "[3/4] Reiniciando API con PM2..."
pm2 restart api-asistencia || pm2 start dist/main.js --name api-asistencia
pm2 save

# 2. Frontend
echo "[4/4] Compilando frontend..."
cd /opt/apps/asistencia/registro/react
npm run build
cp -r dist/* /var/www/asistencia/dist/ 2>/dev/null || mkdir -p /var/www/asistencia/dist && cp -r dist/* /var/www/asistencia/dist/

echo "=== Deploy completado ==="
echo "API: http://localhost:3000"
echo "Web: https://portal.claro.com.ni/asistencia/"
