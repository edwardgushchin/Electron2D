/* Isolated native acquisition fault; selected with LD_LIBRARY_PATH in a child. */
int snd_seq_open(void **handle, const char *name, int streams, int mode) {
    (void)name; (void)streams; (void)mode; *handle = 0; return -13;
}
const char *snd_strerror(int error) { (void)error; return "injected permission denial"; }
int snd_seq_close(void *handle) { (void)handle; return 0; }
