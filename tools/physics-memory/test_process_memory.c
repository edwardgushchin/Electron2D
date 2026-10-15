#include <gnu/libc-version.h>
#include <malloc.h>
#include <stddef.h>
#include <stdio.h>

int main(void)
{
    printf("{\"ABIBytes\":%zu,\"PointerBytes\":%zu,\"UordblksOffset\":%zu,\"HblkhdOffset\":%zu,\"LibcVersion\":\"%s\"}\n",
           sizeof(struct mallinfo2), sizeof(void *), offsetof(struct mallinfo2, uordblks),
           offsetof(struct mallinfo2, hblkhd), gnu_get_libc_version());
    return 0;
}
