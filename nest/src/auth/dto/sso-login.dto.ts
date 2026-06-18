import { IsNotEmpty, IsString } from 'class-validator';

export class SsoLoginDto {
  @IsNotEmpty({ message: 'El token SSO es obligatorio' })
  @IsString()
  readonly token: string;
}
