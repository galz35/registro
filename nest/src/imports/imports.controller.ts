import {
  Controller, Post, Get, Param, UseGuards, UseInterceptors,
  UploadedFile, ParseIntPipe, Req,
} from '@nestjs/common';
import { FileInterceptor } from '@nestjs/platform-express';
import { AuthGuard } from '@nestjs/passport';
import { ImportsService } from './imports.service';
import { Roles } from '../common/roles.decorator';
import { RolesGuard } from '../common/roles.guard';

@Controller('imports')
@UseGuards(AuthGuard('jwt'), RolesGuard)
export class ImportsController {
  constructor(private imports: ImportsService) {}

  @Post('censo/validate')
  @Roles('admin')
  @UseInterceptors(FileInterceptor('archivo'))
  async validateCenso(@UploadedFile() file: Express.Multer.File) {
    return this.imports.validateCenso(file);
  }

  @Post('censo/apply')
  @Roles('admin')
  @UseInterceptors(FileInterceptor('archivo'))
  async applyCenso(@UploadedFile() file: Express.Multer.File, @Req() req: any) {
    return this.imports.applyCenso(file, req.user.carnet);
  }

  @Post('catalogo/validate')
  @Roles('admin')
  @UseInterceptors(FileInterceptor('archivo'))
  async validateCatalogo(@UploadedFile() file: Express.Multer.File) {
    return this.imports.validateCatalogo(file);
  }

  @Post('catalogo/apply')
  @Roles('admin')
  @UseInterceptors(FileInterceptor('archivo'))
  async applyCatalogo(@UploadedFile() file: Express.Multer.File, @Req() req: any) {
    return this.imports.applyCatalogo(file, req.user.carnet);
  }

  @Get(':id/errors')
  @Roles('admin')
  async getErrors(@Param('id', ParseIntPipe) id: number) {
    return this.imports.getErrors(id);
  }
}
