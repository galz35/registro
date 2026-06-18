import {
  Controller, Get, Post, Body, Param, Query, UseGuards, Req,
  ParseIntPipe, UseInterceptors, UploadedFile,
} from '@nestjs/common';
import { FileInterceptor } from '@nestjs/platform-express';
import { AuthGuard } from '@nestjs/passport';
import { DispatchService } from './dispatch.service';
import { DeliverDto } from './dto/deliver.dto';
import { RevertDto } from './dto/revert.dto';
import { Roles } from '../common/roles.decorator';
import { RolesGuard } from '../common/roles.guard';

@Controller('dispatch')
@UseGuards(AuthGuard('jwt'), RolesGuard)
export class DispatchController {
  constructor(private dispatch: DispatchService) {}

  @Get('validate/:hijoId/:jugueteId')
  @Roles('despachador', 'supervisor', 'admin')
  async validate(
    @Param('hijoId', ParseIntPipe) hijoId: number,
    @Param('jugueteId', ParseIntPipe) jugueteId: number,
    @Query('eventoId', ParseIntPipe) eventoId: number,
  ) {
    return this.dispatch.validate(hijoId, jugueteId, eventoId);
  }

  @Post('deliver')
  @Roles('despachador', 'supervisor', 'admin')
  @UseInterceptors(FileInterceptor('foto'))
  async deliver(
    @Body() dto: DeliverDto,
    @Req() req: any,
    @UploadedFile() foto?: Express.Multer.File,
  ) {
    return this.dispatch.deliver(dto, req.user.carnet, foto);
  }

  @Post(':entregaId/revert')
  @Roles('despachador', 'supervisor', 'admin')
  async revert(
    @Param('entregaId', ParseIntPipe) entregaId: number,
    @Body() dto: RevertDto,
    @Req() req: any,
  ) {
    return this.dispatch.revert(entregaId, req.user.carnet, dto.motivo);
  }

  @Get('event/:eventoId/summary')
  @Roles('despachador', 'supervisor', 'admin')
  async summary(@Param('eventoId', ParseIntPipe) eventoId: number) {
    return this.dispatch.getAuditoria(eventoId);
  }
}
