/**************************************************************************/
/*  animation.cpp                                                         */
/**************************************************************************/
/*                         This file is part of:                          */
/*                             GODOT ENGINE                               */
/*                        https://godotengine.org                         */
/**************************************************************************/
/* Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md). */
/* Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.                  */
/*                                                                        */
/* Permission is hereby granted, free of charge, to any person obtaining  */
/* a copy of this software and associated documentation files (the        */
/* "Software"), to deal in the Software without restriction, including    */
/* without limitation the rights to use, copy, modify, merge, publish,    */
/* distribute, sublicense, and/or sell copies of the Software, and to     */
/* permit persons to whom the Software is furnished to do so, subject to  */
/* the following conditions:                                              */
/*                                                                        */
/* The above copyright notice and this permission notice shall be         */
/* included in all copies or substantial portions of the Software.        */
/*                                                                        */
/* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,        */
/* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF     */
/* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. */
/* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY   */
/* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,   */
/* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE      */
/* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                 */
/**************************************************************************/


#include <algorithm>
#include <cmath>
#include <cstdint>
#include <iomanip>
#include <iostream>
#include <stdexcept>
#include <vector>
#define _ALWAYS_INLINE_ inline
#define CMP_EPSILON 0.00001
#define SWAP(a,b) std::swap(a,b)
#define ERR_FAIL_UNSIGNED_INDEX(i,n) if ((uint32_t)(i)>=(n)) throw std::out_of_range("track")
template <typename T> using LocalVector = std::vector<T>;
namespace Math {
using std::abs;
inline double fposmod(double x,double y) { double r=std::fmod(x,y);return r<0?r+y:r; }
inline double fract(double x) { return x-std::floor(x); }
_ALWAYS_INLINE_ bool is_equal_approx(double p_left, double p_right) {
	// Check for exact equality first, required to handle "infinity" values.
	if (p_left == p_right) {
		return true;
	}
	// Then check for approximate equality.
	double tolerance = CMP_EPSILON * abs(p_left);
	if (tolerance < CMP_EPSILON) {
		tolerance = CMP_EPSILON;
	}
	return abs(p_left - p_right) < tolerance;
}
_ALWAYS_INLINE_ double pingpong(double p_value, double p_length) {
	return (p_length != 0.0) ? abs(fract((p_value - p_length) / (p_length * 2.0)) * p_length * 2.0 - p_length) : 0.0;
}
}
class Animation {
public:
enum LoopedFlag {LOOPED_FLAG_NONE,LOOPED_FLAG_END,LOOPED_FLAG_START};
enum {LOOP_NONE,LOOP_LINEAR,LOOP_PINGPONG,TYPE_METHOD,TYPE_AUDIO,TYPE_ANIMATION,FIND_MODE_EXACT};
struct Key { double time; };
struct Track {int type;};
struct MethodTrack:Track {LocalVector<Key> methods;};
struct AudioTrack:Track {LocalVector<Key> values;};
struct AnimationTrack:Track {LocalVector<Key> values;};
LocalVector<Track*> tracks;
double length=1;int loop_mode=LOOP_NONE;
int track_find_key(int i,double time,int) const {auto &v=static_cast<MethodTrack*>(tracks[i])->methods;for(int k=0;k<(int)v.size();k++)if(v[k].time==time)return k;return -1;}
template <typename T> void _track_get_key_indices_in_range(const LocalVector<T>&,double,double,LocalVector<int>*,bool) const;
void track_get_key_indices_in_range(int,double,double,double,double,LocalVector<int>*,LoopedFlag) const;
};
template <typename T>
void Animation::_track_get_key_indices_in_range(const LocalVector<T> &p_array, double from_time, double to_time, LocalVector<int> *r_indices, bool p_is_backward) const {
	int len = p_array.size();
	if (len == 0) {
		return;
	}

	int from = 0;
	int to = len - 1;

	if (!p_is_backward) {
		while (p_array[from].time < from_time || Math::is_equal_approx(p_array[from].time, from_time)) {
			from++;
			if (to < from) {
				return;
			}
		}
		while (p_array[to].time > to_time && !Math::is_equal_approx(p_array[to].time, to_time)) {
			to--;
			if (to < from) {
				return;
			}
		}
	} else {
		while (p_array[from].time < from_time && !Math::is_equal_approx(p_array[from].time, from_time)) {
			from++;
			if (to < from) {
				return;
			}
		}
		while (p_array[to].time > to_time || Math::is_equal_approx(p_array[to].time, to_time)) {
			to--;
			if (to < from) {
				return;
			}
		}
	}

	if (from == to) {
		r_indices->push_back(from);
		return;
	}

	if (!p_is_backward) {
		for (int i = from; i <= to; i++) {
			r_indices->push_back(i);
		}
	} else {
		for (int i = to; i >= from; i--) {
			r_indices->push_back(i);
		}
	}
}
void Animation::track_get_key_indices_in_range(int p_track, double p_time, double p_delta, double p_start, double p_end, LocalVector<int> *r_indices, Animation::LoopedFlag p_looped_flag) const {
	ERR_FAIL_UNSIGNED_INDEX((uint32_t)p_track, tracks.size());

	if (p_delta == 0) {
		return; // Prevent to get key continuously.
	}

	const Track *t = tracks[p_track];

	double from_time = p_time - p_delta;
	double to_time = p_time;
	double start = p_start;
	double end = p_end;
	if (start < 0) {
		start = 0;
	}
	if (end > length) {
		end = length;
	}

	bool is_backward = false;
	if (from_time > to_time) {
		is_backward = true;
		SWAP(from_time, to_time);
	}

	switch (loop_mode) {
		case LOOP_NONE: {
			if (from_time < 0) {
				from_time = 0;
			}
			if (from_time > end) {
				from_time = end;
			}

			if (to_time < 0) {
				to_time = 0;
			}
			if (to_time > end) {
				to_time = end;
			}
		} break;
		case LOOP_LINEAR: {
			if (from_time > end || from_time < start) {
				from_time = Math::fposmod(from_time, end);
			}
			if (to_time > end || to_time < start) {
				to_time = Math::fposmod(to_time, end);
			}

			if (from_time > to_time) {
				// Handle loop by splitting.
				double anim_end = end + CMP_EPSILON;
				double anim_start = start - CMP_EPSILON;

				switch (t->type) {





					case TYPE_METHOD: {
						const MethodTrack *mt = static_cast<const MethodTrack *>(t);
						if (!is_backward) {
							_track_get_key_indices_in_range(mt->methods, from_time, anim_end, r_indices, is_backward);
							_track_get_key_indices_in_range(mt->methods, anim_start, to_time, r_indices, is_backward);
						} else {
							_track_get_key_indices_in_range(mt->methods, anim_start, to_time, r_indices, is_backward);
							_track_get_key_indices_in_range(mt->methods, from_time, anim_end, r_indices, is_backward);
						}
					} break;

					case TYPE_AUDIO: {
						const AudioTrack *ad = static_cast<const AudioTrack *>(t);
						if (!is_backward) {
							_track_get_key_indices_in_range(ad->values, from_time, anim_end, r_indices, is_backward);
							_track_get_key_indices_in_range(ad->values, anim_start, to_time, r_indices, is_backward);
						} else {
							_track_get_key_indices_in_range(ad->values, anim_start, to_time, r_indices, is_backward);
							_track_get_key_indices_in_range(ad->values, from_time, anim_end, r_indices, is_backward);
						}
					} break;
					case TYPE_ANIMATION: {
						const AnimationTrack *an = static_cast<const AnimationTrack *>(t);
						if (!is_backward) {
							_track_get_key_indices_in_range(an->values, from_time, anim_end, r_indices, is_backward);
							_track_get_key_indices_in_range(an->values, anim_start, to_time, r_indices, is_backward);
						} else {
							_track_get_key_indices_in_range(an->values, anim_start, to_time, r_indices, is_backward);
							_track_get_key_indices_in_range(an->values, from_time, anim_end, r_indices, is_backward);
						}
					} break;
				}
				return;
			}

			// Not from_time > to_time but most recent of looping...
			if (p_looped_flag != Animation::LOOPED_FLAG_NONE) {
				if (!is_backward && Math::is_equal_approx(from_time, start)) {
					int edge = track_find_key(p_track, start, FIND_MODE_EXACT);
					if (edge >= 0) {
						r_indices->push_back(edge);
					}
				} else if (is_backward && Math::is_equal_approx(to_time, end)) {
					int edge = track_find_key(p_track, end, FIND_MODE_EXACT);
					if (edge >= 0) {
						r_indices->push_back(edge);
					}
				}
			}
		} break;
		case LOOP_PINGPONG: {
			if (from_time > end || from_time < start) {
				from_time = Math::pingpong(from_time, length);
			}
			if (to_time > end || to_time < start) {
				to_time = Math::pingpong(to_time, length);
			}

			if (p_looped_flag == Animation::LOOPED_FLAG_START) {
				// Handle loop by splitting.
				switch (t->type) {





					case TYPE_METHOD: {
						const MethodTrack *mt = static_cast<const MethodTrack *>(t);
						_track_get_key_indices_in_range(mt->methods, start, from_time, r_indices, true);
						_track_get_key_indices_in_range(mt->methods, start, to_time, r_indices, false);
					} break;

					case TYPE_AUDIO: {
						const AudioTrack *ad = static_cast<const AudioTrack *>(t);
						_track_get_key_indices_in_range(ad->values, start, from_time, r_indices, true);
						_track_get_key_indices_in_range(ad->values, start, to_time, r_indices, false);
					} break;
					case TYPE_ANIMATION: {
						const AnimationTrack *an = static_cast<const AnimationTrack *>(t);
						_track_get_key_indices_in_range(an->values, start, from_time, r_indices, true);
						_track_get_key_indices_in_range(an->values, start, to_time, r_indices, false);
					} break;
				}
				return;
			}
			if (p_looped_flag == Animation::LOOPED_FLAG_END) {
				// Handle loop by splitting.
				switch (t->type) {





					case TYPE_METHOD: {
						const MethodTrack *mt = static_cast<const MethodTrack *>(t);
						_track_get_key_indices_in_range(mt->methods, from_time, end, r_indices, false);
						_track_get_key_indices_in_range(mt->methods, to_time, end, r_indices, true);
					} break;

					case TYPE_AUDIO: {
						const AudioTrack *ad = static_cast<const AudioTrack *>(t);
						_track_get_key_indices_in_range(ad->values, from_time, end, r_indices, false);
						_track_get_key_indices_in_range(ad->values, to_time, end, r_indices, true);
					} break;
					case TYPE_ANIMATION: {
						const AnimationTrack *an = static_cast<const AnimationTrack *>(t);
						_track_get_key_indices_in_range(an->values, from_time, end, r_indices, false);
						_track_get_key_indices_in_range(an->values, to_time, end, r_indices, true);
					} break;
				}
				return;
			}

			// The edge will be pingponged in the next frame and processed there, so let's ignore it now...
			if (!is_backward && Math::is_equal_approx(to_time, end)) {
				to_time -= CMP_EPSILON;
			} else if (is_backward && Math::is_equal_approx(from_time, start)) {
				from_time += CMP_EPSILON;
			}
		} break;
	}
	switch (t->type) {





		case TYPE_METHOD: {
			const MethodTrack *mt = static_cast<const MethodTrack *>(t);
			_track_get_key_indices_in_range(mt->methods, from_time, to_time, r_indices, is_backward);
		} break;

		case TYPE_AUDIO: {
			const AudioTrack *ad = static_cast<const AudioTrack *>(t);
			_track_get_key_indices_in_range(ad->values, from_time, to_time, r_indices, is_backward);
		} break;
		case TYPE_ANIMATION: {
			const AnimationTrack *an = static_cast<const AnimationTrack *>(t);
			_track_get_key_indices_in_range(an->values, from_time, to_time, r_indices, is_backward);
		} break;
	}
}

int main(){
 const double positions[]={-2,-.25,0,.000005,.25,.5,.75,.999995,1,1.000005,1.25,2.5};
 const double deltas[]={-2,-1.25,-.5,-.25,-.000001,0,.000001,.25,.5,1.25,2};
 const double keyTimes[]={0,.000001,.249999,.25,.250001,.5,.75,.999999,1,1.000001,1.25};
 std::cout<<std::setprecision(17);
 for(double length:{1.,2.5,.001})for(int loop=0;loop<3;loop++)for(int flag=0;flag<3;flag++)for(double p:positions)for(double d:deltas){
 Animation a;a.length=length;a.loop_mode=loop;Animation::MethodTrack t;t.type=Animation::TYPE_METHOD;
 for(double k:keyTimes)t.methods.push_back({k*length});a.tracks.push_back(&t);LocalVector<int> result;
 a.track_get_key_indices_in_range(0,p*length,d*length,0,length,&result,(Animation::LoopedFlag)flag);
 std::cout<<length<<'\t'<<loop<<'\t'<<flag<<'\t'<<p*length<<'\t'<<d*length<<'\t';
 if(result.size()==0)std::cout<<'-';
 for(size_t i=0;i<result.size();i++){if(i)std::cout<<',';std::cout<<result[i];}std::cout<<'\n';
 }
}
