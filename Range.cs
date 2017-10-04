using System;
using System.Collections.Generic;
using System.Linq;
using Open.Arithmetic.Dynamic;

namespace Open
{
	public interface IDateTimeIndexed
	{
		DateTime DateTime { get; }
	}


	public interface IRange<out T>
	{
		T Low { get; }
		T High { get; }
	}



	public interface IRangeFlexible<T> : IRange<T>
	{
		void UpdateLow(T value);
		void UpdateHigh(T value);
	}



	public interface IRangeTimeIndexed<out T> : IDateTimeIndexed, IRange<T> { }



	public struct Range<T> : IRange<T>
	{
		private readonly T _high;
		private readonly T _low;

		public Range(T low, T high)
		{
			_low = low;
			_high = high;
		}

		public Range(T equal)
			: this(equal, equal) { }


		#region IRange<TLock> Members
		public T Low
		{
			get { return _low; }
		}

		public T High
		{
			get { return _high; }
		}
		#endregion


		public override string ToString()
		{
			return _low.ToString() + "-" + _high.ToString();
		}

	}

	public struct RangeWithValue<T, TValue> : IRange<T>
	{
		private readonly T _high;
		private readonly T _low;
		private readonly TValue _value;

		public RangeWithValue(T low, T high, TValue value)
		{
			_low = low;
			_high = high;
			_value = value;
		}

		#region IRange<TLock> Members
		public T Low
		{
			get { return _low; }
		}

		public T High
		{
			get { return _high; }
		}

		public TValue Value
		{
			get { return _value; }
		}

		#endregion

		public override string ToString()
		{
			return _low.ToString() + "-" + _high.ToString() + "(" + _value.ToString() + ")";
		}

	}

	public struct RangeTimeIndexed<T> : IRangeTimeIndexed<T>
	{
		private readonly DateTime _dateTime;
		private readonly T _high;
		private readonly T _low;

		public RangeTimeIndexed(DateTime datetime, T low, T high)
		{
			_dateTime = datetime;
			_low = low;
			_high = high;
		}

		public RangeTimeIndexed(DateTime datetime, T equal)
			: this(datetime, equal, equal) { }


		#region IRange<TLock> Members
		public T Low
		{
			get { return _low; }
		}

		public T High
		{
			get { return _high; }
		}
		#endregion


		public override string ToString()
		{
			return _dateTime.ToString() + ":" + _low.ToString() + "-" + _high.ToString();
		}


		#region IDateTimeIndexed Members

		public DateTime DateTime
		{
			get { return _dateTime; }
		}

		#endregion
	}

	public struct RangeTimeIndexedWithValue<T> : IRangeTimeIndexed<T>
	{
		private readonly DateTime _dateTime;
		private readonly T _high;
		private readonly T _low;
		private readonly T _value;

		public RangeTimeIndexedWithValue(DateTime datetime, T low, T high, T value)
		{
			_dateTime = datetime;
			_low = low;
			_high = high;
			_value = value;
		}

		public RangeTimeIndexedWithValue(DateTime datetime, T equal)
			: this(datetime, equal, equal, equal) { }


		#region IRange<TLock> Members
		public T Low
		{
			get { return _low; }
		}

		public T High
		{
			get { return _high; }
		}
		public T Value
		{
			get { return _value; }
		}
		#endregion


		public override string ToString()
		{
			return _dateTime.ToString() + ":" + _low.ToString() + "-" + _high.ToString() + "(" + _value.ToString() + ")";
		}


		#region IDateTimeIndexed Members

		public DateTime DateTime
		{
			get { return _dateTime; }
		}

		#endregion
	}



	public static class RangeExtensions
	{

		public static void RangeValue(this IRangeFlexible<double> target, double value)
		{
			if(target==null)
				throw new NullReferenceException();

			if (double.IsNaN(target.Low) || value < target.Low)
				target.UpdateLow(value);
			if (double.IsNaN(target.High) || value > target.High)
				target.UpdateHigh(value);
		}

		public static Range<DateTime> Range<T>(this IEnumerable<T> items, Func<T, DateTime> selector)
		{
			if(items==null)
				throw new NullReferenceException();
			if(selector==null)
				throw new ArgumentNullException("selector");

			DateTime max = DateTime.MinValue;
			DateTime min = DateTime.MaxValue;

			bool hasItems = false;

			if (items != null)
			{
				foreach (var item in items)
				{
					hasItems = true;
					var value = selector(item);
					if (value < min)
						min = value;
					if (value > max)
						max = value;
				}
			}

			if (!hasItems)
				throw new InvalidOperationException("You cannot acquire a date range from an empty set.");

			return new Range<DateTime>(min, max);
		}

		public static Range<double> Range(this IEnumerable<double> values)
		{
			double max = double.NegativeInfinity;
			double min = double.PositiveInfinity;

			if (values != null)
			{
				foreach (double value in values)
				{
					if (!double.IsNaN(value))
					{
						if (value < min)
							min = value;
						if (value > max)
							max = value;
					}
				}
			}

			return max < min ?
				new Range<double>(double.NaN, double.NaN) :
				new Range<double>(min, max);
		}

		public static Range<double> Range<T>(this IEnumerable<T> items, Func<T, double> selector)
		{
			if(items==null)
				throw new NullReferenceException();
			if(selector==null)
				throw new ArgumentNullException("selector");

			double max = double.NegativeInfinity;
			double min = double.PositiveInfinity;

			if (items != null)
			{
				foreach (var item in items)
				{
					var value = selector(item);
					if (value < min)
						min = value;
					if (value > max)
						max = value;
				}
			}

			return max < min ?
				new Range<double>(double.NaN, double.NaN) :
				new Range<double>(min, max);
		}

		public static Range<double> Range<T>(this ParallelQuery<T> items, Func<T, double> selector)
		{
			if(items==null)
				throw new NullReferenceException();
			if(selector==null)
				throw new ArgumentNullException("selector");

			double max = double.NegativeInfinity;
			double min = double.PositiveInfinity;

			if (items != null)
			{
				object templockMin = new Object(), templockMax = new Object();
				items.ForAll(item =>
			   {
				   double value = selector(item);
				   if (!double.IsNaN(value))
				   {
					   lock (templockMin)
						   if (value < min)
							   min = value;
					   lock (templockMax)
						   if (value > max)
							   max = value;
				   }
			   });
			}

			return max < min ?
				new Range<double>(double.NaN, double.NaN) :
				new Range<double>(min, max);
		}

		#region IRange<float> Arithmetic
		public static IRange<float> AddRange(this IRange<float> r1, IRange<float> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<float>(r1.Low + r2.Low, r1.High + r2.High);
		}

		public static IRange<float> SubtractRange(this IRange<float> r1, IRange<float> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<float>(r1.Low - r2.Low, r1.High - r2.High);
		}

		public static IRange<float> MultiplyByRange(this IRange<float> r1, IRange<float> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<float>(r1.Low * r2.Low, r1.High * r2.High);
		}

		public static IRange<float> DivideByRange(this IRange<float> r1, IRange<float> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<float>(r1.Low / r2.Low, r1.High / r2.High);
		}
		#endregion

		#region IRange<double> Arithmetic
		public static IRange<double> AddRange(this IRange<double> r1, IRange<double> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<double>(r1.Low + r2.Low, r1.High + r2.High);
		}

		public static IRange<double> SubtractRange(this IRange<double> r1, IRange<double> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<double>(r1.Low - r2.Low, r1.High - r2.High);
		}

		public static IRange<double> MultiplyByRange(this IRange<double> r1, IRange<double> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<double>(r1.Low * r2.Low, r1.High * r2.High);
		}

		public static IRange<double> DivideByRange(this IRange<double> r1, IRange<float> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<double>(r1.Low / r2.Low, r1.High / r2.High);
		}
		#endregion

		#region IRange<int> Arithmetic
		public static IRange<int> AddRange(this IRange<int> r1, IRange<int> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<int>(r1.Low + r2.Low, r1.High + r2.High);
		}

		public static IRange<int> SubtractRange(this IRange<int> r1, IRange<int> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<int>(r1.Low - r2.Low, r1.High - r2.High);
		}

		public static IRange<int> MultiplyByRange(this IRange<int> r1, IRange<int> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<int>(r1.Low * r2.Low, r1.High * r2.High);
		}

		public static IRange<int> DivideByRange(this IRange<int> r1, IRange<int> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<int>(r1.Low / r2.Low, r1.High / r2.High);
		}
		#endregion

		#region IRange<TimeSpan> Arithmetic
		public static IRange<TimeSpan> AddRange(this IRange<TimeSpan> r1, IRange<TimeSpan> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<TimeSpan>(r1.Low + r2.Low, r1.High + r2.High);
		}

		public static IRange<TimeSpan> SubtractRange(this IRange<TimeSpan> r1, IRange<TimeSpan> r2)
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<TimeSpan>(r1.Low - r2.Low, r1.High - r2.High);
		}
		#endregion

		#region IRange<IComparable> Arithmetic
		public static IRange<T> AddRange<T>(this IRange<T> r1, IRange<T> r2)
			where T : struct, IComparable
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<T>(r1.Low.AddValue(r2.Low), r1.High.AddValue(r2.High));
		}

		public static IRange<T> SubtractRange<T>(this IRange<T> r1, IRange<T> r2)
			where T : struct, IComparable
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<T>(r1.Low.SubtractValue(r2.Low), r1.High.SubtractValue(r2.High));
		}

		public static IRange<T> MultiplyByRange<T>(this IRange<T> r1, IRange<T> r2)
			where T : struct, IComparable
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<T>(r1.Low.MultiplyBy(r2.Low), r1.High.MultiplyBy(r2.High));
		}

		public static IRange<T> DivideByRange<T>(this IRange<T> r1, IRange<T> r2)
			where T : struct, IComparable
		{
			if(r1==null)
				throw new NullReferenceException();
			if(r2==null)
				throw new ArgumentNullException("r2");

			return new Range<T>(r1.Low.DivideBy(r2.Low), r1.High.DivideBy(r2.High));
		}
		#endregion


		public static T Delta<T>(this IRange<T> target)
			where T : struct, IComparable
		{
			if(target==null)
				throw new NullReferenceException();

			return target.High.SubtractValue(target.Low);
		}

		public static int Delta(this IRange<int> target)
		{
			if(target==null)
				throw new NullReferenceException();

			return target.High - target.Low;
		}

		public static long Delta(this IRange<long> target)
		{
			if(target==null)
				throw new NullReferenceException();

			return target.High - target.Low;
		}

		public static float Delta(this IRange<float> target)
		{
			if(target==null)
				throw new NullReferenceException();

			return target.High - target.Low;
		}

		public static double Delta(this IRange<double> target)
		{
			if(target==null)
				throw new NullReferenceException();

			return target.High - target.Low;
		}

		public static TimeSpan Delta(this IRange<TimeSpan> target)
		{
			if(target==null)
				throw new NullReferenceException();

			return target.High - target.Low;
		}

		public static TimeSpan Delta(this IRange<DateTime> target)
		{
			if(target==null)
				throw new NullReferenceException();

			return TimeSpan.FromTicks(target.High.Ticks - target.Low.Ticks);
		}

		public static bool IsInRange(this IRange<TimeSpan> target, TimeSpan value, bool includeLimits = false)
		{
			if(target==null)
				throw new NullReferenceException();

			return includeLimits ? (value >= target.Low && value <= target.High) : (value > target.Low && value < target.High);
		}

		public static bool IsInRange(this IRange<DateTime> target, DateTime value, bool includeLimits = false)
		{
			if(target==null)
				throw new NullReferenceException();

			return includeLimits ? (value >= target.Low && value <= target.High) : (value > target.Low && value < target.High);
		}

		public static bool IsInRange(this IRange<int> target, int value, bool includeLimits = false)
		{
			if(target==null)
				throw new NullReferenceException();

			return includeLimits ? (value >= target.Low && value <= target.High) : (value > target.Low && value < target.High);
		}

		public static bool IsInRange(this IRange<float> target, float value, bool includeLimits = false)
		{
			if(target==null)
				throw new NullReferenceException();

			return includeLimits ? (value >= target.Low && value <= target.High) : (value > target.Low && value < target.High);
		}

		public static bool IsInRange(this IRange<double> target, double value, bool includeLimits = false)
		{
			if(target==null)
				throw new NullReferenceException();

			return includeLimits ? (value >= target.Low && value <= target.High) : (value > target.Low && value < target.High);
		}

		public static bool IsInRange<T>(this IRange<T> target, T value, bool includeLimits = false)
			where T : IComparable
		{
			if(target==null)
				throw new NullReferenceException();

			var dval = (dynamic)value;
			return includeLimits
				? (dval >= target.Low && dval <= target.High)
				: (dval > target.Low && dval < target.High);
		}


		public static IEnumerable<DateTime> Dates(this IRange<DateTime> target)
		{
			if(target==null)
				throw new NullReferenceException();

			var startDate = target.Low;
			var endDate = target.High;

			for (DateTime d = startDate; d < endDate; d = d.AddDays(1).Date)
				yield return d.Date;
		}

		public static void UpdateMinMax(this double value, ref double min, ref double max)
		{
			if (!double.IsNaN(value))
			{
				if (value < min) min = value;
				if (value > max) max = value;
			}
		}

		public static double Transpose(this double value, double min, double max, double newMin, double newMax)
		{
			if (min == max)
				return double.NaN;

			var oldDelta = max - min;
			var newDelta = newMax - newMin;
			var ratio = newDelta / oldDelta;

			var position = value - min;
			return newMin + position * ratio;
		}
        
        public static double Transpose(this double value, Range<double> source, Range<double> target)
        {
            return value.Transpose(source.Low, source.High, target.Low, target.High);
        }

    }



}

