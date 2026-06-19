import { NestFactory } from '@nestjs/core';
import { ValidationPipe } from '@nestjs/common';
import { NestExpressApplication } from '@nestjs/platform-express';
import * as express from 'express';
import { AppModule } from './app.module';
import { SqlExceptionFilter } from './common/sql-exception.filter';
import helmet from 'helmet';
import * as path from 'path';

async function bootstrap() {
  const app = await NestFactory.create<NestExpressApplication>(AppModule);

  app.setGlobalPrefix('api');

  app.use(helmet());

  app.enableCors({
    origin: process.env.NODE_ENV === 'production' ? process.env.CORS_ORIGIN : '*',
    methods: 'GET,HEAD,PUT,PATCH,POST,DELETE',
    credentials: true,
  });

  app.useGlobalPipes(
    new ValidationPipe({
      whitelist: true,
      forbidNonWhitelisted: true,
      transform: true,
    }),
  );

  app.useGlobalFilters(new SqlExceptionFilter());

  // Servir archivos estaticos (uploads) bajo /uploads
  const uploadPath = path.resolve(process.env.UPLOAD_PATH || './uploads');
  app.use('/asistencia-uploads', express.static(uploadPath as any, { maxAge: '30d' }));

  const port = process.env.PORT || 3000;
  await app.listen(port);
  console.log(`Backend corriendo en puerto ${port}`);
}
bootstrap();
