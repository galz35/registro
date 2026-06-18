import { Controller, Post, Get, Body, UseGuards, Req } from '@nestjs/common';
import { AuthGuard } from '@nestjs/passport';
import { AuthService } from './auth.service';
import { SsoLoginDto } from './dto/sso-login.dto';
import { DevLoginDto } from './dto/dev-login.dto';

@Controller('auth')
export class AuthController {
  constructor(private auth: AuthService) {}

  @Post('sso-login')
  async ssoLogin(@Body() dto: SsoLoginDto) {
    return this.auth.ssoLogin(dto.token);
  }

  @Post('dev-login')
  async devLogin(@Body() dto: DevLoginDto) {
    return this.auth.devLogin(dto.carnet);
  }

  @Get('me')
  @UseGuards(AuthGuard('jwt'))
  async getMe(@Req() req: any) {
    return this.auth.getMe(req.user.carnet);
  }
}
