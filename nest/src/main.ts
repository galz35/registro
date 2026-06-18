import { NestFactory } from '@nestjs/core';
import { ValidationPipe } from '@nestjs/common';
import { NestExpressApplication } from '@nestjs/platform-express';
import { AppModule } from './app.module';
import { SqlExceptionFilter } from './common/sql-exception.filter';
import helmet from 'helmet';

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

  const port = process.env.PORT || 3000;
  await app.listen(port);
  console.log(`Backend corriendo en puerto ${port}`);
}
bootstrap();
