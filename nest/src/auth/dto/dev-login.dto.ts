import { IsNotEmpty, IsString } from 'class-validator';

export class DevLoginDto {
  @IsNotEmpty()
  @IsString()
  readonly carnet: string;

  @IsNotEmpty()
  @IsString()
  readonly password: string;
}
