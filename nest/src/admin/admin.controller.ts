import { Controller, Get, Post, Body, Query, UseGuards } from '@nestjs/common';
import { AuthGuard } from '@nestjs/passport';
import { AdminService } from './admin.service';
import { Roles } from '../common/roles.decorator';
import { RolesGuard } from '../common/roles.guard';

@Controller('admin')
@UseGuards(AuthGuard('jwt'), RolesGuard)
export class AdminController {
  constructor(private admin: AdminService) {}

  @Get('users')
  @Roles('admin')
  async getUserRoles() {
    return this.admin.getUserRoles();
  }

  @Get('search-portal')
  @Roles('admin')
  async searchPortal(@Query('q') q: string) {
    return this.admin.searchUsers(q || '');
  }

  @Post('set-role')
  @Roles('admin')
  async setRole(@Body() dto: { carnet: string; rol: string }) {
    return this.admin.setRole(dto.carnet, dto.rol);
  }
}
