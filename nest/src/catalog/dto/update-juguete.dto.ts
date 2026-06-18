import { IsOptional, IsString, IsInt, Min } from 'class-validator';
import { Type } from 'class-transformer';

export class UpdateJugueteDto {
  @IsOptional()
  @IsString()
  readonly categoria?: string;

  @IsOptional()
  @IsString()
  readonly genero?: string;

  @IsOptional()
  @IsString()
  readonly nombreJuguete?: string;

  @IsOptional()
  @Type(() => Number)
  @IsInt()
  @Min(0)
  readonly stockInicial?: number;
}
