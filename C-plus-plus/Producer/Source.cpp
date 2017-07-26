// Explicitly stops producers
// Note: Uses an atomic to set the quit flag.
#include <atomic>
#include <condition_variable>
#include <cstdlib>
#include <fstream>
#include <iostream>
#include <mutex>
#include <queue>
#include <thread>
#include <array>
#include <chrono>
#include <sstream>
#include <Windows.h>
using namespace std;

class ProducerConsumer {
	static const size_t TEN = 10;
	static array<int, 10> MyArray;
	static queue<int> q, q2;
	static condition_variable q_cond, second_q_cond;
	static mutex q_sync, print, second_q_sync, second_print;
	static atomic_size_t nprod, second_nprod;
	static ofstream output, second_output;
public:
	static const size_t nprods = 4, ncons = 3;
	static atomic_bool quit, second_quit;
	static void report()
	{
		for (size_t i = 0; i < MyArray.size(); i++)
		{
			cout << "Group " << i << " has " << MyArray[i] << " numbers" << endl;
		}
	}
	static void group(size_t i)
	{
		for (;;)
		{
			unique_lock<mutex> mylck(second_q_sync);
			second_q_cond.wait(mylck, [](){return !q2.empty() || quit.load(); });
			if (quit){ break; }
			auto y = q2.front();
			if (y % TEN == i)
			{
				q2.pop();
				mylck.unlock();
				ostringstream my_string_stream;
				my_string_stream << "Bin" << i << ".txt";
				ofstream OutFile(my_string_stream.str(), ios::app);
				OutFile << y << endl;
				OutFile.close();
				MyArray[i]++;
			}
		}
	}
	static void consume() {
		for (;;) {
			// Get lock for sync mutex
			unique_lock<mutex> qlck(q_sync);

			// Check for end of program (no live producers, no data left)
			if (nprod.load() == 0 && q.empty())
				break;

			// Wait for queue to have something to process
			q_cond.wait(qlck, [](){return !q.empty() || quit.load(); });
			if (quit){ break; }
			auto x = q.front();
			q.pop();
			qlck.unlock();

			// Print trace of consumption
			unique_lock<mutex> slck(second_q_sync);
			q2.push(x);
			slck.unlock();
			second_q_cond.notify_all();
		}
		--second_nprod;
		second_q_cond.notify_all();
	}
	static void produce() {
		srand(time(NULL));
		// Generate random ints indefinitely
		for (;;) {
			// See if it's time to quit
			if (quit.load())
				break;

			unique_lock<mutex> slck(q_sync);
			int n = rand();     // Get random int

			// Get lock for sync mutex; push int
			q.push(n);
			slck.unlock();
			q_cond.notify_one();

			// Get lock for print mutex
			lock_guard<mutex> plck(print);
			//output << n << " produced" << endl;
		}

		// Notify consumers that a producer has shut down
		--nprod;
		q_cond.notify_all();    // Is a lock needed here?
	}
};
queue<int> ProducerConsumer::q;
condition_variable ProducerConsumer::q_cond;
mutex ProducerConsumer::q_sync, ProducerConsumer::print;
atomic_bool ProducerConsumer::quit;
ofstream ProducerConsumer::output("wait6.out");
atomic_size_t ProducerConsumer::nprod(nprods);
//for second queue
queue<int> ProducerConsumer::q2;
condition_variable ProducerConsumer::second_q_cond;
mutex ProducerConsumer::second_q_sync, ProducerConsumer::second_print;
atomic_bool ProducerConsumer::second_quit;
atomic_size_t ProducerConsumer::second_nprod(ncons);
array<int, 10> ProducerConsumer::MyArray;
int main() {
	srand(time(NULL));
	vector<thread> prods, cons;
	array<thread, 10> bins;
	for (int i = 0; i < ProducerConsumer::ncons; ++i)
		cons.push_back(thread(&ProducerConsumer::consume));
	for (int i = 0; i < ProducerConsumer::nprods; ++i)
		prods.push_back(thread(&ProducerConsumer::produce));
	for (int i = 0; i < 10; ++i)
		bins[i] = thread(&ProducerConsumer::group, i);

	cout << "Press Enter to quit...";
	cin.get();
	ProducerConsumer::quit = true;

	// Join all threads
	for (auto &p : prods)
		p.join();
	for (auto &c : cons)
		c.join();
	for (auto &b : bins)
		b.join();

	ProducerConsumer::report();

	system("PAUSE");
	return 0;
}