import { Controller, Get, Post, Body, Param, Query, UseGuards, Req, ParseIntPipe } from '@nestjs/common';
import { AuthGuard } from '@nestjs/passport';
import { AttendanceService } from './attendance.service';
import { RegisterAttendanceDto } from './dto/register-attendance.dto';
import { Roles } from '../common/roles.decorator';
import { RolesGuard } from '../common/roles.guard';

@Controller('attendance')
@UseGuards(AuthGuard('jwt'), RolesGuard)
export class AttendanceController {
  constructor(private attendance: AttendanceService) {}

  @Get('lookup/:carnet')
  @Roles('despachador', 'supervisor', 'admin')
  async lookup(@Param('carnet') carnet: string, @Query('eventoId', ParseIntPipe) eventoId: number) {
    return this.attendance.lookupCarnet(carnet, eventoId);
  }

  @Post('register')
  @Roles('despachador', 'supervisor', 'admin')
  async register(@Body() dto: RegisterAttendanceDto, @Req() req: any) {
    return this.attendance.register(dto, req.user.carnet);
  }

  @Post('revert')
  @Roles('despachador', 'supervisor', 'admin')
  async revert(@Body() dto: RegisterAttendanceDto) {
    return this.attendance.revertAttendance(dto.eventoId, dto.carnet);
  }

  @Get('event/:eventoId/summary')
  @Roles('despachador', 'supervisor', 'admin')
  async summary(@Param('eventoId', ParseIntPipe) eventoId: number) {
    return this.attendance.getSummary(eventoId);
  }

  @Get('censo')
  @Roles('despachador', 'supervisor', 'admin')
  async censo(
    @Query('eventoId', ParseIntPipe) eventoId: number,
    @Query('busqueda') busqueda?: string,
    @Query('estado') estado?: string,
    @Query('pagina', ParseIntPipe) pagina = 1,
    @Query('porPagina', ParseIntPipe) porPagina = 50,
  ) {
    return this.attendance.getCenso(eventoId, busqueda, estado, pagina, porPagina);
  }
}
