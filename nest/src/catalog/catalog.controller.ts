import {
  Controller, Get, Post, Put, Patch, Delete, Body, Param, Query,
  UseGuards, UseInterceptors, UploadedFile, ParseIntPipe,
} from '@nestjs/common';
import { FileInterceptor } from '@nestjs/platform-express';
import { AuthGuard } from '@nestjs/passport';
import { CatalogService } from './catalog.service';
import { CreateJugueteDto } from './dto/create-juguete.dto';
import { UpdateJugueteDto } from './dto/update-juguete.dto';
import { Roles } from '../common/roles.decorator';
import { RolesGuard } from '../common/roles.guard';

@Controller('catalog')
@UseGuards(AuthGuard('jwt'), RolesGuard)
export class CatalogController {
  constructor(private catalog: CatalogService) {}

  @Get()
  @Roles('despachador', 'supervisor', 'admin')
  async getAll() {
    return this.catalog.getAll();
  }

  @Get('summary')
  @Roles('despachador', 'supervisor', 'admin')
  async getSummary() {
    return this.catalog.getSummary();
  }

  @Post()
  @Roles('despachador', 'supervisor', 'admin')
  @UseInterceptors(FileInterceptor('foto'))
  async create(@Body() dto: CreateJugueteDto, @UploadedFile() foto?: Express.Multer.File) {
    return this.catalog.create(dto, foto);
  }

  @Put(':id')
  @Roles('despachador', 'supervisor', 'admin')
  @UseInterceptors(FileInterceptor('foto'))
  async update(
    @Param('id', ParseIntPipe) id: number,
    @Body() dto: UpdateJugueteDto,
    @UploadedFile() foto?: Express.Multer.File,
  ) {
    return this.catalog.update(id, dto, foto);
  }

  @Patch(':id/deactivate')
  @Roles('despachador', 'supervisor', 'admin')
  async deactivate(@Param('id', ParseIntPipe) id: number) {
    return this.catalog.deactivate(id);
  }

  @Post(':id/photo')
  @Roles('despachador', 'supervisor', 'admin')
  @UseInterceptors(FileInterceptor('foto'))
  async uploadPhoto(@Param('id', ParseIntPipe) id: number, @UploadedFile() foto: Express.Multer.File) {
    return this.catalog.uploadPhoto(id, foto);
  }
}
