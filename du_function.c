#include <dirent.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <stdio.h>
#include <errno.h>
#include <stdlib.h>

#define THOUSAND 10000

int Print_directory(char* direct_name);

int main(int argc, char* argv[])
{
  int check_for_option;
  char* option = "-n";
  char* choice = argc >1 ? argv[1] : ".";

  struct stat detail_entry;
  if(stat(choice, &detail_entry) != 0)
    {
      printf("Unable to open %s file or directory\n", choice);
      return 0;
    }
  printf("%lld bytes\t%s\n", (long long)detail_entry.st_size ,choice);
  if(S_ISDIR(detail_entry.st_mode))
    {
      return Print_directory(choice);
    }
  return 0;
}

int Print_directory(char* direct_name)
{
  DIR* current_dir = opendir(direct_name);
  char direct[THOUSAND] = {0};
  if(current_dir == NULL)
    {
      printf("Unable to open %s directory or file\n", direct_name);
      return 1;
    }
  struct dirent* direct_ent;
  while((direct_ent = readdir(current_dir)) != NULL)
    {
      if(!strcmp(direct_ent->d_name, "."))
	{
	  continue;
	}
      if(!strcmp(direct_ent->d_name, ".."))
	{
	  continue;
	}
      snprintf(direct, sizeof(direct)-1, "%s/%s", direct_name, direct_ent->d_name);
      struct stat detail_entry;
      if(stat(direct, &detail_entry) != 0)
	{
	  printf("Unable to open %s file or directory\n", direct_name);
	  continue;
	}
      printf("%lld bytes\t%s\n", (long long)detail_entry.st_size, direct);
      if(S_ISDIR(detail_entry.st_mode))
	{
	  if(Print_directory(direct) != 0)
	    {
	      closedir(current_dir);
	      return 1;
	    }
	}
    }
  closedir(current_dir);
  return 0;
}
