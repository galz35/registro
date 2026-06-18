import { Controller, Get } from '@nestjs/common';
import { DatabaseService } from './database/database.service';

@Controller('health')
export class HealthController {
  constructor(private db: DatabaseService) {}

  @Get()
  async check() {
    const dbStatus = await this.db.healthCheck();
    return {
      status: dbStatus.ok ? 'ok' : 'degraded',
      timestamp: new Date().toISOString(),
      database: dbStatus.ok ? 'connected' : dbStatus.error,
    };
  }
}
