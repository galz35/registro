import { IsNotEmpty, IsString, IsInt, Min } from 'class-validator';
import { Type } from 'class-transformer';

export class CreateJugueteDto {
  @IsNotEmpty()
  @IsString()
  readonly categoria: string;

  @IsNotEmpty()
  @IsString()
  readonly genero: string;

  @IsNotEmpty()
  @IsString()
  readonly nombreJuguete: string;

  @IsNotEmpty()
  @Type(() => Number)
  @IsInt()
  @Min(0)
  readonly stockInicial: number;
}
