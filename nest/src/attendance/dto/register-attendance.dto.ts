import { IsNotEmpty, IsString, IsInt, IsOptional, Min, IsEnum, ValidateIf } from 'class-validator';
import { Type } from 'class-transformer';

export enum AsistioPorEnum {
  COLABORADOR = 'COLABORADOR',
  CONYUGE = 'CONYUGE',
  TERCERO = 'TERCERO',
}

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

  @IsOptional()
  @IsEnum(AsistioPorEnum)
  readonly asistioPor?: AsistioPorEnum;

  @ValidateIf(o => o.asistioPor === AsistioPorEnum.TERCERO)
  @IsNotEmpty({ message: 'Debe ingresar el nombre de quien asistio' })
  @IsString()
  readonly nombreAsistente?: string;
}
