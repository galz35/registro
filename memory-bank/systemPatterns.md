# System Patterns

Patrones obligatorios:

- NestJS modular.
- DatabaseModule con pool `mssql`.
- SQL parametrizado.
- SQL Server `OUTPUT INSERTED` para devolver filas insertadas/actualizadas.
- Transacciones explicitas para despacho y reversion.
- JWT interno despues del SSO Portal.
- Imagenes como WebP en disco.
- Importaciones con batch y errores auditables.

Patrones prohibidos:

- ORMs o query builders.
- SQL concatenado con input del usuario.
- secretos hardcodeados.
- fotos Base64 en SQL Server.
- doble entrega activa por hijo/evento.
- stock negativo.

