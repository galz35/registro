import { Module } from '@nestjs/common';
import { HttpModule } from '@nestjs/axios';
import { AttendanceController } from './attendance.controller';
import { AttendanceService } from './attendance.service';
import { HcmService } from '../integration/hcm.service';

@Module({
  imports: [HttpModule],
  controllers: [AttendanceController],
  providers: [AttendanceService, HcmService],
  exports: [AttendanceService],
})
export class AttendanceModule {}
