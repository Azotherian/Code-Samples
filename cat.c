#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#define LINELEN 24
#define PAGELEN 512

void in_to_out()
{
  char line[LINELEN];
  while(fgets(line,LINELEN,stdin))
    {
      printf("%s",line);
    }
}

int print_to_screen(char* filename, int use_numbers)
{
  char line[LINELEN];
  int line_number = 1;
  FILE* file_pointer = fopen(filename,"r");
  if(file_pointer == NULL)
    {
      printf("Sorry, that file doesn't exist\n");
      return 0;
    }
  while(1)
    {
      char* get_line = fgets(line, LINELEN, file_pointer);
      if(get_line)
	{
	  if(use_numbers)
	    {
	      printf("%d %s", line_number,line);
	    }
	  else
	    {
	      printf("%s", line);
	    }
	}
      else if(get_line == NULL)
	{
	  break;
	}
      else
	{
	  printf("Can't find a match\n");
	}
      line_number++;
    }
  fclose(file_pointer);
  return 1;
}

int main (int argc, char *argv[])
{
  char* file_name;
  char* check_for_n = "-n";
  int compare_n = 0;
  int position = 0;
  if(argc == 1)
    {
      in_to_out(stdin); //send stdin to stdout
    }
  else
    {
      argc--; //remove first command from list
      compare_n = strncmp(argv[1], check_for_n, 1);
      if(compare_n == 0) //check if there is an option
	{
	  compare_n = strncmp(argv[1], check_for_n, 2);
	  if(compare_n == 0) //check if the option is "-n"
	    {
	      argc--; // remove option from list
	      position = 2; //move to first file
	      while(argc > 0)
		{
		  file_name = argv[position];
		  print_to_screen(file_name,1);
		  position++;
		  argc--;
		}
	    }
	  else
	    {
	      printf("The option is not allowed with this command\n");
	    }
	}
      else
	{
	  position = 1; //move to position to the first file
	  while(argc > 0)
	    {
	      file_name = argv[position];
	      print_to_screen(file_name, 0);
	      position++;
	      argc--;
	    }
	}
    }
}
