import { ExceptionFilter, Catch, ArgumentsHost, HttpStatus } from '@nestjs/common';
import { Response } from 'express';

@Catch()
export class SqlExceptionFilter implements ExceptionFilter {
  catch(exception: any, host: ArgumentsHost) {
    const ctx = host.switchToHttp();
    const response = ctx.getResponse<Response>();

    if (exception.message?.includes('SQL Server')) {
      const message = exception.message;
      let status = HttpStatus.INTERNAL_SERVER_ERROR;
      let customMessage = 'Error interno en el servidor de base de datos.';

      if (message.includes('1205')) {
        status = HttpStatus.CONFLICT;
        customMessage = 'Conflicto de concurrencia. Reintente la operacion.';
      } else if (message.includes('547')) {
        status = HttpStatus.BAD_REQUEST;
        customMessage = 'Error de integridad referencial. Datos no coinciden.';
      } else if (message.includes('2627') || message.includes('2601')) {
        status = HttpStatus.CONFLICT;
        customMessage = 'El registro ya existe en el sistema.';
      }

      response.status(status).json({
        statusCode: status,
        timestamp: new Date().toISOString(),
        message: customMessage,
      });
      return;
    }

    if (exception.getStatus && exception.getResponse) {
      const status = exception.getStatus();
      const res = exception.getResponse();
      response.status(status).json(res);
      return;
    }

    response.status(HttpStatus.INTERNAL_SERVER_ERROR).json({
      statusCode: HttpStatus.INTERNAL_SERVER_ERROR,
      timestamp: new Date().toISOString(),
      message: 'Error interno del servidor.',
    });
  }
}
