import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { DatabaseModule } from './database/database.module';
import { AuthModule } from './auth/auth.module';
import { AttendanceModule } from './attendance/attendance.module';
import { CatalogModule } from './catalog/catalog.module';
import { DispatchModule } from './dispatch/dispatch.module';
import { ImportsModule } from './imports/imports.module';
import { ReportsModule } from './reports/reports.module';
import { HealthController } from './health.controller';

@Module({
  imports: [
    ConfigModule.forRoot({ isGlobal: true }),
    DatabaseModule,
    AuthModule,
    AttendanceModule,
    CatalogModule,
    DispatchModule,
    ImportsModule,
    ReportsModule,
  ],
  controllers: [HealthController],
})
export class AppModule {}
