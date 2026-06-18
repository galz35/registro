import { IsNotEmpty, IsInt, IsString, IsEnum, ValidateIf } from 'class-validator';
import { Type } from 'class-transformer';

export enum RecibidoPorEnum {
  COLABORADOR = 'COLABORADOR',
  CONYUGE = 'CONYUGE',
  TERCERO = 'TERCERO',
}

export class DeliverDto {
  @IsNotEmpty()
  @Type(() => Number)
  @IsInt()
  readonly eventoId: number;

  @IsNotEmpty()
  @Type(() => Number)
  @IsInt()
  readonly hijoId: number;

  @IsNotEmpty()
  @Type(() => Number)
  @IsInt()
  readonly jugueteId: number;

  @IsNotEmpty()
  @IsString()
  readonly carnetColaborador: string;

  @IsNotEmpty()
  @IsEnum(RecibidoPorEnum)
  readonly recibidoPor: RecibidoPorEnum;

  @ValidateIf((o) => o.recibidoPor === RecibidoPorEnum.TERCERO)
  @IsNotEmpty({ message: 'Debe ingresar el nombre del tercero' })
  @IsString()
  readonly nombreReceptor?: string;
}
