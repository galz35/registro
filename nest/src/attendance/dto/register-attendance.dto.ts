import { IsNotEmpty, IsString, IsInt, IsOptional, Min } from 'class-validator';
import { Type } from 'class-transformer';

export class RegisterAttendanceDto {
  @IsNotEmpty()
  @IsInt()
  readonly eventoId: number;

  @IsNotEmpty()
  @IsString()
  readonly carnet: string;

  @IsOptional()
  @Type(() => Number)
  @IsInt()
  @Min(0)
  readonly adultos?: number;

  @IsOptional()
  @Type(() => Number)
  @IsInt()
  @Min(0)
  readonly ninos?: number;
}
