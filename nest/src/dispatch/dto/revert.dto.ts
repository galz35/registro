import { IsNotEmpty, IsString, MinLength } from 'class-validator';

export class RevertDto {
  @IsNotEmpty({ message: 'El motivo de la reversion es obligatorio' })
  @IsString()
  @MinLength(10, { message: 'El motivo debe tener al menos 10 caracteres' })
  readonly motivo: string;
}
