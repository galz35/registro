import { Controller, Get, Header, Param, Query, UseGuards, ParseIntPipe } from '@nestjs/common';
import { AuthGuard } from '@nestjs/passport';
import { ReportsService } from './reports.service';
import { Roles } from '../common/roles.decorator';
import { RolesGuard } from '../common/roles.guard';

@Controller('reports')
@UseGuards(AuthGuard('jwt'), RolesGuard)
export class ReportsController {
  constructor(private reports: ReportsService) {}

  @Get('entregas.csv')
  @Roles('despachador', 'supervisor', 'admin')
  @Header('Content-Type', 'text/csv; charset=utf-8')
  @Header('Content-Disposition', 'attachment; filename="entregas.csv"')
  async getEntregasCSV(@Query('eventoId', ParseIntPipe) eventoId: number) {
    return this.reports.getEntregasCSV(eventoId);
  }

  @Get('pendientes.csv')
  @Roles('despachador', 'supervisor', 'admin')
  @Header('Content-Type', 'text/csv; charset=utf-8')
  @Header('Content-Disposition', 'attachment; filename="pendientes.csv"')
  async getPendientesCSV(@Query('eventoId', ParseIntPipe) eventoId: number) {
    return this.reports.getPendientesCSV(eventoId);
  }

  @Get('inventario.csv')
  @Roles('despachador', 'supervisor', 'admin')
  @Header('Content-Type', 'text/csv; charset=utf-8')
  @Header('Content-Disposition', 'attachment; filename="inventario.csv"')
  async getInventarioCSV() {
    return this.reports.getInventarioCSV();
  }
}
