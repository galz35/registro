module.exports = {
  apps: [{
    name: 'api-asistencia',
    script: 'dist/main.js',
    cwd: '/opt/apps/asistencia/registro/nest',
    instances: 1,
    exec_mode: 'fork',
    env: {
      NODE_ENV: 'production',
      PORT: 3000,
    },
    env_file: '.env',
    max_memory_restart: '500M',
    log_file: '/var/log/pm2/api-asistencia.log',
    error_file: '/var/log/pm2/api-asistencia-error.log',
    merge_logs: true,
    autorestart: true,
    watch: false,
  }],
};
