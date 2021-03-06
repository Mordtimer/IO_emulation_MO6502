﻿using EmulatorMS6502;
using System;
using System.Collections.Generic;
using System.Text;

// OPCODE
namespace EmulatorMOS6502.CPU
{
	using UInt8 = Byte;

	public partial class MOS6502
	{
		//Tu implementujemy wszystkie opcody

		/// <summary>
		/// LDA  Load Accumulator with Memory - wpisz daną wartość z podanej lokalizacji na akumulator	
		/// </summary>
		bool LDA()
		{
			Fetch();
			a = fetched;
			//Console.WriteLine($"--a {a}--");
			// Jeśli na jest 0	
			setFlag('Z', a == 0x00);
			// Jeśli 8 bit jest równy 'zapalony'	
			setFlag('N', (Byte)(a & 0x80) == 0x80);
			return true;
		}

		/// <summary>
		/// AND Memory with Accumulator - bitowe AND dla tego co pobrane z pamięci oraz Akumulatora	
		/// </summary>
		bool AND()
		{
			// Pobieramy dane do zmiennej fetched	
			Fetch();
			// Wykonujemy bitowe AND dla akumulatora	
			a &= fetched;
			// Ustawiamy dwie flagi Z oraz N	
			// Jeśli wynikiem jest 0	
			setFlag('Z', a == 0x00);
			// Jeśli 8 bit jest równy 'zapalony'	
			setFlag('N', Convert.ToBoolean(a & 0x80));

			// Instrukcja może zwrócić dodatkowy bit, jeśli granica strony jest przekroczona to dodajemy +1 cykl	
			return true;
		}

		/// <summary>
		/// BEQ Branch on Result Zero - wykonanie innej instrukcji skacząc o wartość adresu względnego (relative)	
		/// Jeśli flaga Z jest równa 1
		/// </summary>
		bool BEQ()
		{
			if (getFlag('Z') == 1)
			{
				// Podaj do wykonania lokalizacje po skoku z wzgędnego adresu	
				absAddress = (UInt16)(programCounter + relAddress);
				// Jeśli na tej samej stronie to zwiększ cykle o 1	
				// Jeśli przekroczył strone to cykle +2	
				if ((absAddress & 0xFF00) != (programCounter & 0xFF00))
				{
					cycles += 2;
				}
				else
				{
					cycles++;
				}

				// Nadaj do wykonania na program counter adres po skoku	
				programCounter = absAddress;
			}

			return false;
		}

		/// <summary>
		/// BPL Branch on Result Plus - wykonanie innej instrukcji skacząc o wartość adresu względnego (relative)
		/// </summary>
		bool BPL()
		{
			if (getFlag('N') == 0)
			{
				// Podaj do wykonania lokalizacje po skoku z wzgędnego adresu	
				absAddress = (UInt16)(programCounter + relAddress);
				// Jeśli na tej samej stronie to zwiększ cykle o 1	
				// Jeśli przekroczył strone to cykle +2	
				if ((absAddress & 0xFF00) != (programCounter & 0xFF00))
				{
					cycles += 2;
				}
				else
				{
					cycles++;
				}

				// Nadaj do wykonania na program counter adres po skoku	
				programCounter = absAddress;
			}

			return false;
		}

		/// <summary>
		/// CLC Clear Carry Flag - instrukcja czyszczenia flagi carry
		/// </summary>
		bool CLC()
		{
			setFlag('C', false);
			return false;
		}

		/// <summary>
		/// CMP Compare Memory with Accumulator - porównanie akumulatora z pamięcią
		/// </summary>
		bool CMP()
		{
			// Ładujemy dane	
			Fetch();
			// Wykonujemy A(cumulator) - M(emory)	
			Byte score = (Byte)(a - fetched); //1111 1111 - 0000 0000 = 0000 0000 1111 1111 

			// Odpowiednio podnosimy flagi:	
			// Jeśli wynikiem jest 0 = są takie same	
			setFlag('Z', !Convert.ToBoolean(score));

			// Jeśli wynik ujemny = M > A	
			setFlag('N', Convert.ToBoolean(score & 0x80));

			// Jeśli A > M	
			setFlag('C', a >= fetched);

			return true;
		}

		/// <summary>
		/// DEX Decrement Index X by One - obniżanie wartości rejestru X
		/// </summary>
		bool DEX()
		{
			// Obniżamy rejestr X
			x--;
			// Podnosimy odpowiednie flagi jeśli warunki spełnione
			setFlag('N', Convert.ToBoolean(x & 0x80));
			setFlag('Z', x == 0x00);

			return false;
		}

		/// <summary>
		/// INX Increment Index X by One - podnoszenie wartości rejestru X
		/// </summary>
		bool INX()
		{
			// Podnosimy wartość i ustawiamy flagi jeśli warunki spełnione
			x++;
			setFlag('N', Convert.ToBoolean(x & 0x80));
			setFlag('Z', x == 0x00);

			return false;
		}

		/// <summary>
		/// NOP No Operation
		/// </summary>
		bool NOP()
		{
			return false;
		}

		/// <summary>
		/// PLA Pull Accumulator from Stack - 
		/// Zebranie ze stosu i wczytanie z lokalizacji do akumulatora 
		/// Stos domyślnie zapisany jest na stronie 0x01
		/// </summary>
		// i MOS 6502 używa stosu malejącego tzn. że stos rośnie w dół (stack pointer maleje przy push a podnosi się przy pull)
		bool PLA()
		{
			// Ściągamy ze stosu przesuwając wskaźnik
			stackPointer++;
			// Wpisujemy na akumulator to co znajdowało się pod adresem z wskaźnika (ze strony 1)
			a = ReadFromBus((UInt16)(0x0100 + stackPointer));

			// Podnosimy odpowiednie flagi jeśli warunki spełnione
			setFlag('N', Convert.ToBoolean(a & 0x80));
			setFlag('Z', a == 0x00);

			return false;
		}

		/// <summary>
		/// RTI Return from Interrupt -
		/// Instrukcja przywraca stan flag które były ustawione na status register (N, Z, C, I, D, V)
		/// oraz przywraca program counter
		/// </summary>
		bool RTI()
		{
			// Ściągamy ze stosu i przywracamy flagi
			stackPointer++;
			statusRegister = ReadFromBus((UInt16)(0x0100 + stackPointer));

			// Pozostałe flagi takie jak Break oraz nieużywana (1<<5) nie powinny być ustawione więc je wyłączamy
			// Chcemy zgasić B=(1<<4) i (1<<5) więc:
			// 'B'     00010000
			// (1<<5)  00100000
			//	48     00110000
			//  207    11001111
			statusRegister &= 207;

			// Ściągamy ze stosu kolejno składając 16 bitowy program counter w całość
			stackPointer++;
			UInt16 tmp = (UInt16)(ReadFromBus((UInt16)(0x0100 + stackPointer)));

			stackPointer++;
			programCounter = (UInt16)(((UInt16)(ReadFromBus((UInt16)(0x0100 + stackPointer))) << 8) | tmp);

			return false;
		}

		/// <summary>
		/// STY Sore Index Y in Memory - przechowaj wartość rejestru Y pod danym adresem w pamięci
		/// </summary>
		bool STY()
		{
			WriteToBus(absAddress, y);
			return false;
		}

		/// <summary>
		/// TXA Transfer Index X to Accumulator - przenieś wartość z rejestru X do akumulatora
		/// </summary>
		bool TXA()
		{
			a = x;

			// Podnieś odpowiednie flagi jeśli spełnione warunki
			setFlag('N', Convert.ToBoolean(a & 0x80));
			setFlag('Z', a == 0x00);

			return false;
		}

		/// <summary>
		/// SED Set Decimal Flag - podnosi flage decimal
		/// </summary>
		bool SED()
		{
			setFlag('D', true);
			return false;
		}

		/// <summary>
		/// ADC Add Memory to Accumulator with Carry - Bitowa operacja dodawania
		/// </summary>
		bool ADC() //Dodawanie
		{
			Fetch();

			//wynik powinien być w UInt8 ale ustawiamy na UInt16 żeby było prościej zaznaczyć flagi (przy dodawaniu można np. wyjść poza zakres)
			UInt16 result = (UInt16)((UInt16)a + (UInt16)fetched + (UInt16)getFlag('C'));

			// trzeba ustawić carry bit jeżeli wynik jest większy niż 255 (bo wynik i tak musi być podany w UInt8 a tutaj mamy UInt16)
			if (result > 255)
				setFlag('C', true);
			else
				setFlag('C', false);


			//jezeli wynik jest rowny 0 to ustawiamy flagę zero na true
			bool parameterZ = ((result & 0x00FF) == 0);
			setFlag('Z', parameterZ);

			//poniższe działanie sprawdza czy nie nastąpił overflow i powinno zwrócić wartość true w dwóch przypadkach: 
			// dodatnia+dodatnia==ujemna
			// ujemna+ujemna==dodatnia
			bool parameterV =
				Convert.ToBoolean((UInt16)((~((UInt16)a ^ (UInt16)fetched) & ((UInt16)a ^ (UInt16)result)) &
											(UInt16)0x0080));
			setFlag('V', parameterV);

			//pierwszy bit oznacza liczbę ujemną, więc jeżeli jest pierwszy bit to ustawiamy negative na true
			if (Convert.ToBoolean(result & 0x80))
				setFlag('N', true);
			else
				setFlag('N', false);

			//zapisujemy wynik do akumulatora z uwzględnieniem zamiany liczby na UInt8 (był potrzebny UInt16 żeby było łatwiej)
			a = (UInt8)(result & 0x00FF);

			return true; //czy te returny sa bez znaczenia?//-Nie, mają znaczenie -- PJ
		}

		/// <summary>
		/// BCS Branch on Carry Set - wykonanuje instrukcję jedynie jeżeli carry bit = true
		/// </summary>
		bool BCS()
		{
			if (getFlag('C') == 1)
			{
				cycles++;
				absAddress = (UInt16)(programCounter + relAddress);

				if ((absAddress & 0xFF00) != (programCounter & 0xFF00))
					cycles++;

				programCounter = absAddress;
			}

			return false;
		}

		/// <summary>
		/// BNE Branch on Result not Zero - wykonuje instrukcję jedynie jeżeli nie ma ustawionej flagi zero
		/// </summary>
		bool BNE()
		{
			if (getFlag('Z') == 0)
			{
				absAddress = (UInt16)(programCounter + relAddress);

				//przekroczenie "page boundary" skutkuje zużyciem dodatkowego cyklu
				if ((absAddress & 0xFF00) != (programCounter & 0xFF00))
				{
					cycles++;
				}

				programCounter = absAddress;

				cycles++;
			}

			return false;
		}

		/// <summary>
		/// BVS Branch on Overflow Set - wykonanie instrukcji jeżeli branch nie jest overflow
		/// </summary>
		bool BVS()
		{
			if (getFlag('V') == 1)
			{
				cycles++;
				absAddress = (UInt16)(programCounter + relAddress);

				//przekroczenie "page boundary" skutkuje zużyciem dodatkowego cyklu
				if ((absAddress & 0xFF00) != (programCounter & 0xFF00))
				{
					cycles++;
				}

				programCounter = absAddress;
			}

			return false;
		}

		/// <summary>
		/// CLV Clear Ocerflow Flag - wyczyszczenie flagi overflow (ustawia ją na false)
		/// </summary>
		bool CLV()
		{
			setFlag('V', false);
			return false;
		}

		/// <summary>
		/// Decrement Index X by One - obniża wartość w pamięci o jeden
		/// </summary>
		bool DEC()
		{
			Fetch();

			UInt8 result = (UInt8)(fetched - 1);

			//zapisanie wartosci do bus'a
			WriteToBus(absAddress, result);

			//ustawienie flagi zero jezeli wyszlo nam zero
			setFlag('Z', result == 0x0000);

			//jeżeli bit odpowiedzialny za wskazywanie wartości ujemnej jest 1 to wtedy ustawiamy flagę n
			setFlag('N', Convert.ToBoolean(result & 0x0080));

			return false;
		}

		/// <summary>
		/// INC Increment Memory by One - podnosi wartość pamięci o jeden
		/// </summary>
		bool INC()
		{
			Fetch();

			UInt8 result = (UInt8)(fetched + 1);

			//zapisanie wartosci do bus'a
			WriteToBus(absAddress, result);

			//ustawiamy flage zero jezeli wyszlo zero
			setFlag('Z', result == 0x0000);

			//ustawiamy flage negative jezeli wyszla wartosc ujemna
			setFlag('N', Convert.ToBoolean(result & 0x0080));

			return false;
		}

		/// <summary>
		/// JSR Jump to New Location Saving Return Address - zapisuje programCounter do bus'a i wczytuje na niego absolute address
		/// </summary>
		bool JSR()
		{
			//zapisuje programCounter do bus'a i wczytuje na niego absolute address

			programCounter--;

			UInt16 left8bitsOfProgramCounter = (UInt16)(programCounter >> 8);
			Bus.Instance.WriteToBus((UInt16)(0x0100 + stackPointer), (UInt8)(left8bitsOfProgramCounter & 0x00FF));
			stackPointer -= 1;

			Bus.Instance.WriteToBus((UInt16)(0x0100 + stackPointer), (UInt8)(programCounter & 0x00FF));
			stackPointer -= 1;

			programCounter = absAddress;

			return false;
		}

		/// <summary>
		/// LSR Shift One Bit Right - przesuwa jeden bit w prawo (operacja przesunięcia bitowego)
		/// </summary>
		bool LSR()
		{
			Fetch();
			//ostatni bit wrzucamy jako carry bit
			setFlag('C', Convert.ToBoolean(fetched & 0x0001));

			//teraz mozemy przesunac cala wartosc o 1 bo ostatni bit byl uzyty wyzej
			UInt8 fetchedOneRight = (UInt8)(fetched >> 1);

			if ((fetchedOneRight & 0x00FF) == 0x0000)
				setFlag('Z', true);
			else
				setFlag('Z', false);

			if (Convert.ToBoolean(fetchedOneRight & 0x0080))
				setFlag('N', true);
			else
				setFlag('N', false);

			if (lookup[opcode].AdressingMode == IMP)
				a = (byte)(fetchedOneRight & 0x00FF);
			else
				WriteToBus(absAddress, (byte)(fetchedOneRight & 0x00FF));

			return false;
		}

		/// <summary>
		/// PHP Push Processor Status on Stack - zapisuje status flag (register status) na drugiej stronie pamięci
		/// </summary>
		bool PHP()
		{
			//zapisujemy statusRegister
			WriteToBus((UInt16)(0x0100 + stackPointer), (UInt8)(statusRegister | (1 << 4) | (1 << 5)));
			stackPointer--;

			//resetujemy obie flagi
			setFlag('B', false);
			setFlag('U', false);

			return false;
		}

		/// <summary>
		/// ROR Rotate Right - Rotate One bit Left. Przesuwa wszystkie bajty o jeden w prawo
		/// </summary>
		bool ROR()
		{
			Fetch();

			UInt16 temp = (UInt16)((getFlag('C') << 7) | (fetched >> 1));
			if (Convert.ToBoolean(fetched & 0x01))
				setFlag('C', true);
			else
				setFlag('C', false);

			if (Convert.ToBoolean((temp & 0x00FF) == 0x00))
				setFlag('Z', true);
			else
				setFlag('Z', false);

			if (Convert.ToBoolean(temp & 0x0080))
				setFlag('N', true);
			else
				setFlag('N', false);

			if (lookup[opcode].AdressingMode == IMP)
				a = (byte)(temp & 0x00FF);
			else
				WriteToBus(absAddress, (byte)(temp & 0x00FF));
			return false;
		}

		/// <summary>
		/// SEC Set Carry Flag - ustawia flagę carry na true
		/// </summary>
		bool SEC()
		{
			//ustawienie flagi carry bit na true

			setFlag('C', true);
			return false;
		}

		/// <summary>
		/// STX Store Index X in memory - zapisuje x na absolutnym adresie
		/// </summary>
		bool STX()
		{
			//zapisanie x na adresie abs

			WriteToBus(absAddress, x);
			return false;
		}

		/// <summary>
		/// TSX Transfer accumulator to X - zapisuje stackPointer pod x i ustawia flagi Z i N jezeli zajdzie taka potrzeba
		/// </summary>
		bool TSX()
		{
			//zapisuje stackPointer pod x'ksem i ustawia flagi Z i N jezeli zajdzie taka potrzeba

			x = stackPointer;

			if (stackPointer == 0x00)
				setFlag('Z', true);
			else
				setFlag('Z', false);

			if (Convert.ToBoolean(stackPointer & 0x80))
				setFlag('N', true);
			else
				setFlag('N', false);

			return false;
		}

		/// <summary>
		/// BCC Branch if Carry Clear - Jeżeli flaga C jest ustawiona na  0 to wtedy ustawiamy PC na odpowiedni adres
		/// </summary>
		bool BCC()
		{
			if (getFlag('C') == 0)
			{
				cycles++;
				absAddress = (UInt16)(programCounter + relAddress);

				if ((absAddress & 0xFF00) != (programCounter & 0xFF00))
					cycles++;

				programCounter = absAddress;
			}

			return false;
		}

		/// <summary>
		/// BMI Branch if negative - jeżeli flaga N jest ustawiona na 1 to ustawiamy PC na odpowiedni adres
		/// </summary>
		bool BMI()
		{
			if (getFlag('N') == 1)
			{
				cycles++;
				absAddress = (UInt16)(programCounter + relAddress);

				if ((absAddress & 0xFF00) != (programCounter & 0xFF00))
					cycles++;

				programCounter = absAddress;
			}

			return false;
		}

		/// <summary>
		/// BVC Branch if Overflow Clear - jeżeli flaga V jest ustawiona na 0 to ustawiamy PC na odpowiedni adres
		/// </summary>
		bool BVC()
		{
			if (getFlag('V') == 0)
			{
				cycles++;
				absAddress = (UInt16)(programCounter + relAddress);

				if ((absAddress & 0xFF00) != (programCounter & 0xFF00))
					cycles++;

				programCounter = absAddress;
			}

			return false;
		}

		/// <summary>
		/// CLI Clear Interrupt Flag (Disable Interrupts) - Ustawia flage I na 0
		/// </summary>
		bool CLI()
		{
			setFlag('I', false);
			return false;
		}

		/// <summary>
		/// CPY Compare Y Register - Zmienia flagi N, C, Z
		/// </summary>
		bool CPY()
		{
			Fetch();
			var tmp = (UInt16)y - (UInt16)fetched;
			setFlag('C', y >= fetched);
			setFlag('Z', (tmp & 0x00FF) == 0x0000);
			setFlag('N', (tmp & 0x0080) == 0x0000);

			return false;
		}

		/// <summary>
		/// CPX Compare X Register - Zmienia flagi N, C, Z
		/// </summary>
		bool CPX()
		{
			Fetch();
			var tmp = (UInt16)x - (UInt16)fetched;
			setFlag('C', y >= fetched);
			setFlag('Z', (tmp & 0x00FF) == 0x0000);
			setFlag('N', (tmp & 0x0080) == 0x0000);

			return false;
		}

		/// <summary>
		/// EOR Bitowa operacja XOR - Zmienia flagi Negative i Zero jeśli to konieczne
		/// </summary>
		bool EOR()
		{
			Fetch();
			a = (Byte)(a ^ fetched);
			setFlag('Z', a == 0x00);
			setFlag('N', Convert.ToBoolean(a & 0x80));
			return true;
		}

		/// <summary>
		/// JMP Jump To Location - skacze do lokacji czyli ustawia Program Counter na absolute address
		/// </summary>
		bool JMP()
		{
			programCounter = absAddress;
			return false;
		}

		/// <summary>
		/// LDY Load The Y Register - wczytuje rejestr y z fetched potencjalnie zmienia flagi N i Z
		/// </summary>
		bool LDY()
		{
			Fetch();
			y = fetched;
			setFlag('Z', a == 0x00);
			setFlag('N', Convert.ToBoolean(a & 0x80));
			return true;
		}

		/// <summary>
		/// PHA Push acuumulator to Stack - po prostu używa funkcji WriteToBus
		/// </summary>
		bool PHA()
		{
			WriteToBus((UInt16)(0x0100 + stackPointer), a);
			stackPointer--;
			return false;
		}

		/// <summary>
		/// ROL Rotate One bit Left. Przy każdej przsesuwa bity o jeden w lewo
		/// </summary>
		bool ROL()
		{
			Fetch();
			var tmp = (UInt16)(fetched << 1) | getFlag('C');
			setFlag('C', Convert.ToBoolean(tmp & 0xFF00));
			setFlag('Z', (tmp & 0xFF00) == 0x0000);
			setFlag('N', Convert.ToBoolean(tmp & 0x0080));
			if (lookup[opcode].AdressingMode == IMP)
				a = (byte)(tmp & 0x00FF);
			else
				WriteToBus(absAddress, (byte)(tmp & 0x00FF));
			return false;

		}

		/// <summary>
		/// Zapisuje rejsestr x do absAddress
		/// </summary>
		bool STA()
		{
			WriteToBus(absAddress, a);
			return false;
		}

		/// <summary>
		/// TAY Transfer Accumulator to Y Register
		/// </summary>

		bool TAY()
		{
			y = a;
			setFlag('Z', y == 0x00);
			setFlag('N', Convert.ToBoolean(y & 0x80));
			return false;
		}

		/// <summary>
		/// TYA Transfer Index Y to Accumulator - Przekazuje rejest Y do akumulatora 
		/// </summary>
		bool TYA()
		{
			a = y;
			setFlag('Z', y == 0x00);
			setFlag('N', Convert.ToBoolean(y & 0x80));
			return false;
		}

		/// <summary>
		/// SBC Subtract Memory from Accumulator with Borrow - odejmowanie
		/// </summary>
		bool SBC()
		{
			Fetch();
			// Po prostu używamy XORa i zamieniamy liczbe dodatnią na ujemną i wykonujemy po prostu dodawanie
			UInt16 negativeValue = Convert.ToUInt16((UInt16)fetched ^ 0x00FF);

			// wynik powinien być w UInt8 ale ustawiamy na UInt16 żeby było prościej zaznaczyć flagi (przy dodawaniu można np. wyjść poza zakres)
			UInt16 result = (UInt16)((UInt16)a + (UInt16)negativeValue + (UInt16)getFlag('C'));

			// trzeba ustawić carry bit jeżeli wynik jest większy niż 255 (bo wynik i tak musi być podany w UInt8 a tutaj mamy UInt16)
			if (result > 255)
				setFlag('C', true);
			else
				setFlag('C', false);

			// jezeli wynik jest rowny 0 to ustawiamy flagę zero na true
			bool parameterZ = ((result & 0x00FF) == 0);
			setFlag('Z', parameterZ);

			// poniższe działanie sprawdza czy nie nastąpił overflow i powinno zwrócić wartość true w dwóch przypadkach: 
			// dodatnia+dodatnia==ujemna
			// ujemna+ujemna==dodatnia
			bool parameterV =
				Convert.ToBoolean((UInt16)((~((UInt16)a ^ (UInt16)negativeValue) & ((UInt16)a ^ (UInt16)result)) &
											(UInt16)0x0080));
			setFlag('V', parameterV);

			// pierwszy bit oznacza liczbę ujemną, więc jeżeli jest pierwszy bit to ustawiamy negative na true
			if (Convert.ToBoolean(result & 0x80))
				setFlag('N', true);
			else
				setFlag('N', false);

			// zapisujemy wynik do akumulatora z uwzględnieniem zamiany liczby na UInt8 (był potrzebny UInt16 żeby było łatwiej)
			a = (UInt8)(result & 0x00FF);

			return true;
		}

		/// <summary>
		/// XXX - Funkcja obejmująca nielegalne opcody
		/// </summary>
		bool XXX()
		{
			return false;
		}

		/// <summary>
		/// RTS Return from Subroutine - odczytuje program counter
		/// </summary>
		bool RTS() 
		{
			stackPointer++;
			programCounter = (UInt16)ReadFromBus((UInt16)(0x0100 + stackPointer));
			stackPointer++;
			programCounter |= Convert.ToUInt16((UInt16)ReadFromBus((UInt16)(0x0100 + stackPointer)) << 8);
			programCounter++;
			return false;
		}

		/// <summary>
		/// ASL Shift Left One Bit - operacja przesunięcia bitowego o 1 w lewo
		/// </summary>
		bool ASL()
		{
			Fetch();
			var tmp = (UInt16)fetched << 1;

			setFlag('C', (tmp & 0xFF00) > 0);
			setFlag('Z', (tmp & 0x00FF) == 0x00);
			setFlag('N', Convert.ToBoolean(tmp & 0x80)); //nie jestem pewien, ale ja jestem poprzednia osobo pisząca komentarz :)
			if (lookup[opcode].AdressingMode == IMP)
				a = (byte)(tmp & 0x00FF);
			else
				WriteToBus(absAddress, (byte)(tmp & 0x00FF));
			return false;
		}

		/// <summary>
		/// BIT Test Bits in Memory with Accumulator - ustawia flagi N, Z, V na podstawie danych z pamięci
		/// </summary>
		bool BIT()
		{
			Fetch();
			var tmp = a & fetched;
			setFlag('Z', (tmp & 0x00FF) == 0x00);
			setFlag('N', Convert.ToBoolean(fetched & (1 << 7)));
			setFlag('V', Convert.ToBoolean(fetched & (1 << 6)));//01000000
			return false;
		}

		/// <summary>
		/// BRK Force Break - Podnosi flagę interupt i break
		/// </summary>
		bool BRK()
		{
			programCounter++;
			setFlag('I', true);
			WriteToBus((UInt16)(0x0100 + stackPointer), (byte)((programCounter >> 8) & 0x00FF));
			stackPointer--;
			WriteToBus((UInt16)(0x0100 + stackPointer), (byte)(programCounter & 0x00FF));
			stackPointer--;

			setFlag('B', true);
			WriteToBus((UInt16)(0x0100 + stackPointer), statusRegister);
			stackPointer--;
			setFlag('B', false);

			programCounter = (UInt16)(ReadFromBus(0xFFFE) | (UInt16)(ReadFromBus(0xFFFF) << 8));
			return false;
		}

		/// <summary>
		/// CLD Clear Decimal Mode - czyści flagę decimal
		/// </summary>
		bool CLD()
		{
			setFlag('D', false);
			return false;
		}

		/// <summary>
		/// DEY Decrement Index Y by One - obniża x o jeden
		/// </summary>
		bool DEY()
		{
			y--;
			setFlag('Z', y == 0x00);
			setFlag('N', Convert.ToBoolean(y & 0x80));
			return false;
		}

		/// <summary>
		/// INY Increment Index Y by One - podwyższa y o jeden
		/// </summary>
		bool INY()
		{
			y++;
			setFlag('Z', y == 0x00);
			setFlag('N', Convert.ToBoolean(y & 0x80));
			return false;
		}

		/// <summary>
		/// LDX Load Index X with Memory - ładuje dane z fetch do x
		/// </summary>
		bool LDX()
		{
			Fetch();
			x = fetched;
			setFlag('Z', x == 0x00);
			setFlag('N', Convert.ToBoolean(x & 0x80));
			return true;
		}

		/// <summary>
		/// ORA OR Memory with Accumulator - wykonuje bitowe or na akumulatorze i miejscu w pamięci
		/// </summary>
		bool ORA()
		{
			Fetch();
			a = (byte)(a | fetched);
			setFlag('Z', a == 0x00);
			setFlag('N', Convert.ToBoolean(a & 0x80));
			return false;
		}

		/// <summary>
		/// PLP Pull Processor Status from Stack - odczytuje rejestr flag
		/// </summary>
		bool PLP()
		{
			stackPointer++;
			statusRegister = ReadFromBus((UInt16)(0x0100 + stackPointer));
			return false;
		}

		bool RST()
		{
			stackPointer++;
			programCounter = ReadFromBus((UInt16)(0x0100 + stackPointer));
			stackPointer++;
			programCounter |= (UInt16)(ReadFromBus((UInt16)(0x0100 + stackPointer)) << 8);

			programCounter++;
			return false;
		}

		/// <summary>
		/// SEI Set Interrupt Disable Status - ustawia flage interupt na true
		/// </summary>
        
		bool SEI()
		{
			setFlag('I', true);
			return false;
		}

		/// <summary>
		/// TAX Transfer Accumulator to Index X - przekazuje acumulator do rejestru x
		/// </summary>
		bool TAX()
		{
			x = a;
			setFlag('Z', x == 0x00);
			setFlag('N', Convert.ToBoolean(x & 0x80));
			return false;
		}

		/// <summary>
		/// TSX Transfer Stack Pointer to Index X - przekazuje stac pointer do rejestru x
		/// </summary>
		bool TXS()
		{
			stackPointer = x;
			return false;
		}
	}
}
