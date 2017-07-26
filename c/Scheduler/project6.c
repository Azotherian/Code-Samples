#include <dirent.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <stdio.h>
#include <errno.h>
#include <inttypes.h>

#define SIZE 100
#define HUNTHOU 100000
#define HUNDRED 100

void First_Come_First_Served(int PID_Array[], int Arrive_Time[], int Complete_Array[], int size)
{
  printf("\nPID\tWait\tTurnaround");
  int i = 0;
  int current_time = Complete_Array[i];
  int wait_time = 0;
  int turnaround_time = Complete_Array[i];
  double wait_total = wait_time;
  double turnaround_total = turnaround_time;
  printf("\n%d\t%d\t%d", PID_Array[i], wait_time, turnaround_time);
  for(i = 1; i < size; i++)
    {
      wait_time = (current_time - Arrive_Time[i]);
      turnaround_time = wait_time + Complete_Array[i];
      current_time += Complete_Array[i];
      wait_total += wait_time;
      turnaround_total += turnaround_time;
      printf("\n%d\t%d\t%d", PID_Array[i], wait_time, turnaround_time);
    }
  printf("\nAverage Wait Time: %.1f and Average Turnaround Time: %.1f", wait_total/size, turnaround_total/size);
}

int Lowest_Index_Ready(int current_index, int size, int current_time, int Arrive_Time[])
{
  int j = current_index;
  for(j; j < size; j++)
    {
      if(Arrive_Time[j] > current_time)
	{
	  return j;
	}
    }
  return size;
}

void Shortest_Job_First(int a[], int b[], int c[], int size)
{
  printf("\nPID\tWait\tTurnaround");
  int i = 0;
  int temporary = 0;
  int swap_index = 0;
  int current_time = c[i];
  int wait_time = 0;
  int turnaround_time = current_time;
  int shortest_time = HUNTHOU;
  int temporary_process = 0;
  int temporary_arrive_time = 0;
  int temporary_completion_time = 0;
  double wait_total = wait_time;
  double turnaround_total = turnaround_time;
  int Arrive_Time[size];
  int Complete_Array[size];
  int PID_Array[size];
  for(i = 0; i < size; i++)
    {
      PID_Array[i] = a[i];
      Arrive_Time[i] = b[i];
      Complete_Array[i] = c[i];
    }
  i = 0;
  printf("\n%d\t%d\t%d", PID_Array[i], wait_time, turnaround_time);
  for(i = 1; i < size; i++)
    {
      int j = Lowest_Index_Ready(i, size, current_time, Arrive_Time);
      if(j != i)
	{
	  for(temporary = i; temporary < j; temporary++)
	    {
	      if(Complete_Array[temporary] < shortest_time)
		{
		  shortest_time = Complete_Array[temporary];
		  swap_index = temporary;
		}
	    }
	  shortest_time = HUNTHOU; //reset shortest time so loop will still work
	  temporary_process = PID_Array[i];
	  temporary_arrive_time = Arrive_Time[i];
	  temporary_completion_time = Complete_Array[i];
	  PID_Array[i] = PID_Array[swap_index];
	  Arrive_Time[i] = Arrive_Time[swap_index];
	  Complete_Array[i] = Complete_Array[swap_index];
	  PID_Array[swap_index] = temporary_process;
	  Arrive_Time[swap_index] = temporary_arrive_time;
	  Complete_Array[swap_index] = temporary_completion_time;
	}
      //last job is in order
      wait_time = (current_time - Arrive_Time[i]);
      turnaround_time = wait_time + Complete_Array[i];
      current_time += Complete_Array[i];
      wait_total += wait_time;
      turnaround_total += turnaround_time;
      printf("\n%d\t%d\t%d", PID_Array[i], wait_time, turnaround_time);
    }
  printf("\nAverage Wait Time: %.1f and Average Turnaround Time: %.1f", wait_total/size, turnaround_total/size);
}

void Shortest_Remaining_Time_Next(int PID_Array[], int Arrive_Time[], int Complete_Array[], int size)
{
  printf("\nPID\tWait\tTurnaround");
  int i;
  int current_time;
  int Remaining_Time[size];
  int Wait_Time[size];
  int Turnaround_Time[size];
  for(i = 0; i < size; i++)
    {
      Remaining_Time[i] = Complete_Array[i];
      Wait_Time[i] = -1;
      Turnaround_Time[i] = -1;
    }
  for(current_time = 0; current_time < HUNDRED; current_time++)
    {
      int process_to_run = -1;
      int smallest_complete_time = HUNTHOU;
      for(i = 0; i < size; i++)
	{
	  if(current_time >= Arrive_Time[i])
	    {
	      if(Remaining_Time[i] > 0 && Remaining_Time[i] < smallest_complete_time)
		{
		  smallest_complete_time = Remaining_Time[i];
		  process_to_run = i;
		}
	    }
	}
      if(process_to_run != -1)
	{
	  Remaining_Time[process_to_run] = Remaining_Time[process_to_run] - 1;
	  if(Wait_Time[process_to_run] == -1)
	    {
	      Wait_Time[process_to_run] = current_time - Arrive_Time[process_to_run];
	    }
	  if(Remaining_Time[process_to_run] == 0)
	    {
	      Turnaround_Time[process_to_run] = current_time - Arrive_Time[process_to_run] + 1;
	    }
	}
    }
  double wait_total = 0;
  double turnaround_total = 0;
  printf("PID\tWait\tTurnaround\n");
  for(i = 0; i < size; i++)
    {
      printf("%d\t%d\t%d\n", PID_Array[i], Wait_Time[i], Turnaround_Time[i]);
      wait_total += Wait_Time[i];
      turnaround_total += Turnaround_Time[i];
    }
  printf("\nAverage Wait Time: %.1f and Average Turnaround Time: %.1f\n\n", wait_total/size, turnaround_total/size);
}

int main(int argc, char* argv[])
{
  int Arrive_Time[SIZE];
  int Complete_Array[SIZE];
  int PID_Array[SIZE];
  int i = 0;
  int size = 0;
  do
    {
      printf("\n");
      int temporary;
      printf("\nEnter arrival times in ms, enter -1 to stop: ");
      scanf("%d", &temporary);
      if(temporary == -1)
	{
	  break;
	}
      Arrive_Time[i] = temporary;
      printf("Enter time to completion in ms: ");
      scanf("%d", &temporary);
      Complete_Array[i] = temporary;
      PID_Array[i] = i + 1;
      i++;
    }while(i != -1);
  size = i;
  printf("\nProcess ID\tArrival Time(ms)\tTime To Completion(ms)");
  for(i = 0; i < size; i++)
    {
      printf("\n%d\t\t%d\t\t\t%d", PID_Array[i], Arrive_Time[i], Complete_Array[i]);
    }
  printf("\n\nFirst Come, First Served");
  First_Come_First_Served(PID_Array, Arrive_Time, Complete_Array, size);
  printf("\n\nShortest Job First");
  Shortest_Job_First(PID_Array, Arrive_Time, Complete_Array, size);
  printf("\n\nShortest Remaining Time Next");
  Shortest_Remaining_Time_Next(PID_Array, Arrive_Time, Complete_Array, size);
  return 1;
}
