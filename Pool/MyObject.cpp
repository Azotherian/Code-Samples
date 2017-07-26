#include "MyObject.h"

Pool MyObject::myPool{sizeof(MyObject)};

ostream& operator<<(ostream& bob, const MyObject& s)
{
	return bob << '{' << s.id << ',' << s.name << '}';
}
