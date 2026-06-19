import { Controller, Get, Header, Param, Query, UseGuards, ParseIntPipe, Res } from '@nestjs/common';
import type { Response } from 'express';
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

  @Get('asistencia.xlsx')
  @Roles('despachador', 'supervisor', 'admin')
  async getAsistenciaExcel(@Query('eventoId', ParseIntPipe) eventoId: number, @Res() res: Response) {
    const buffer = await this.reports.generateAsistenciaExcel(eventoId);
    res.set({ 'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'Content-Disposition': 'attachment; filename="reporte_asistencia.xlsx"' });
    res.send(buffer);
  }

  @Get('despacho.xlsx')
  @Roles('despachador', 'supervisor', 'admin')
  async getDespachoExcel(@Query('eventoId', ParseIntPipe) eventoId: number, @Res() res: Response) {
    const buffer = await this.reports.generateDespachoExcel(eventoId);
    res.set({ 'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'Content-Disposition': 'attachment; filename="reporte_despacho.xlsx"' });
    res.send(buffer);
  }

  @Get('inventario.xlsx')
  @Roles('despachador', 'supervisor', 'admin')
  async getInventarioExcel(@Res() res: Response) {
    const buffer = await this.reports.generateInventarioExcel();
    res.set({ 'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'Content-Disposition': 'attachment; filename="reporte_inventario.xlsx"' });
    res.send(buffer);
  }
}
