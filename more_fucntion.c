#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <string.h>
#include <termios.h>
#include <fcntl.h>
#include <signal.h>
#include <sys/types.h>

struct termios savestate, oldstate;
int  fd_tty;

void handle_signal(int signum)
{
    //restore flags
    oldstate.c_lflag |= ICANON | ECHO;
    //set the terminal attributes
    tcsetattr(fd_tty, TCSANOW, &savestate);
    printf("\n\nTerminal settings have been restored. Terminating...!\n");
    exit(0);
}

int get_value(FILE* fp)
{
    int c;
    while(1)
    {
        c = getc(fp);
        if(c == ' ') break;
        if(c == '\n') break;
        if(c == 'q') break;
    }
    return c;
}

void save_attributes()
{
    fd_tty = open("/dev/tty", O_RDONLY);
    tcgetattr(fd_tty, &oldstate);
    savestate = oldstate;

    //turn off canonical mode and echo
    oldstate.c_lflag &= ~(ICANON | ECHO);
    oldstate.c_cc[VMIN] = 1;

    //set terminal attributes
    tcsetattr(fd_tty, TCSANOW, &oldstate);
}

void reset_attributes()
{
    fd_tty = open("/dev/tty", O_RDONLY);

    printf("\nRestoring attributes..\n");
    oldstate.c_lflag |= ICANON | ECHO;

    //set the terminal attributes
    tcsetattr(fd_tty, TCSANOW, &savestate);
}

void stop_reverse_video()
{
  printf("\r                                                       \r");
}

int main(int argc, const char* argv[])
{
    FILE* fp;
    double percent = 0.0;
    char file_name[] = "test.txt";
    struct stat st;
    int file_size, count = 0, c, read_char, counter = 1, line_index;
    int printed_size = 0, is_printed = 0, space_hit = 0, cr_hit = 0;
    char line[1000]; //line buffer
    save_attributes();
    signal(SIGINT, handle_signal);
    //if no argument given, take input from stdin
    if(argc == 1)
    {
      fd_tty = open("/dev/tty", O_RDONLY);
      fp = fdopen(fd_tty, "r");      
      while((read_char = getc(stdin)) != EOF)
	{
	  int index = 0;
	  int i;
	  while(index < 3)
	    {
	      for(i = 0; i < 80 ; i++)
		{
		  fputc(read_char, stdout);
		  read_char = getc(stdin);
		}
	      printf("\n");
	      index++;
	    }
	  
	  if(space_hit == 0)
	    {
	      printf("\033[7m%d bytes have been displayed. Please enter an option: \033[m", 240);
	      space_hit = 1;
	    }
	  
	  else if(cr_hit)
	    {
	      printf("\033[7m%d bytes have been displayed. Please enter an option: \033[m", 80);
	      cr_hit = 0;
	    }
	  
	  c = get_value(fp);
	  
	  if(c == ' ')
	    {
	      space_hit = 0;
	      counter = 1;    //clear counter and print 3 more lines
	      stop_reverse_video();
	      continue;
	    }
	  
	  else if (c == 'q') break;   //exit
	  
	  else if(c == '\n')
	    {   
	      cr_hit = 1;
	      line_index = 1;
	      stop_reverse_video();
	      if(fgets(line, 80, fp) != NULL);
	      {
		printf("%s", line);
		printed_size += strlen(line);
	      }
	    }
	}	  
      reset_attributes();
    }
    else if (argc > 1)
      {
        if((fp = fopen(file_name, "r")) == NULL)
	  {
            printf("File could not be opened!");
            exit(1);
	  }	
        if(stat(file_name, &st) == 0)
	  {
            file_size = st.st_size;
	  }	
        while(fgets(line, 80, fp) != NULL)
	  {
	    printf("%s", line);
	    printed_size += strlen(line);
	    do
	      {		
		if(fgets(line, 80, fp) != NULL)
		  {
		    printf("%s", line);
		    printed_size += strlen(line);
		  }
	      }while(count++ < 21);
	    if(is_printed == 0)
	      {
		percent = 100*printed_size/file_size;
		printf("\033[7m%s %.1f%%\033[m", file_name, percent);
		is_printed = 1;
	      }	    
	    else
	      {
		if(space_hit){space_hit = 0;}
    		else{cr_hit = 0;}
		percent = 100*printed_size/file_size;
                printf("\033[7m%.1f%%\033[m", percent);
	      }
	    
            c = get_value(stdin);
	    
            if(c == ' ')
	      {
                space_hit = 1;
                stop_reverse_video();
		count = 0;
		do
		  {
		    if(fgets(line, 80, fp) != NULL)
		      {
			printf("%s", line);
			printed_size += strlen(line);
		      }
		  }while(count++ < 20);
                continue;
	      }	    
            else if (c == 'q') {break;}	    
            else if(c == '\n')
	      {
                cr_hit = 1;
                stop_reverse_video();
                if(fgets(line, 80, fp) != NULL)
		  {
		    fprintf(stdout,"\r%s", line); //do not reset count if carriage return entered
		    printed_size += strlen(line);
		  }
	      }
	  }   
        //close the file
        fclose(fp);
      }    
    reset_attributes();    
    return 0;
}




