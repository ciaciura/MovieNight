using System;
using System.Linq.Expressions;

namespace Shared.Models.Base
{
public enum MappingDirection
	{
		ToModel,
		ToView
	}

	public sealed class MappingRegistry<TView, TModel>
	{
		private readonly Dictionary<MappingDirection, Dictionary<string, LambdaExpression>> _mappingsByDirection =
			new()
			{
				[MappingDirection.ToModel] = new(StringComparer.Ordinal),
				[MappingDirection.ToView] = new(StringComparer.Ordinal)
			};

		public MappingRegistry<TView, TModel> AddToModel(string mappingName, Expression<Func<TView, TModel>> projection)
		{
			Add(MappingDirection.ToModel, mappingName, projection);
			return this;
		}

		public MappingRegistry<TView, TModel> AddToView(string mappingName, Expression<Func<TModel, TView>> projection)
		{
			Add(MappingDirection.ToView, mappingName, projection);
			return this;
		}

		internal IReadOnlyDictionary<MappingDirection, IReadOnlyDictionary<string, LambdaExpression>> Build()
		{
			var result = new Dictionary<MappingDirection, IReadOnlyDictionary<string, LambdaExpression>>();
			foreach (var directionEntry in _mappingsByDirection)
			{
				result[directionEntry.Key] = directionEntry.Value;
			}

			return result;
		}

		private void Add(MappingDirection direction, string mappingName, LambdaExpression projection)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(mappingName);
			ArgumentNullException.ThrowIfNull(projection);

			var directionMap = _mappingsByDirection[direction];
			if (!directionMap.TryAdd(mappingName, projection))
			{
				throw new InvalidOperationException(
					$"A mapping for direction '{direction}' and name '{mappingName}' is already registered.");
			}
		}
	}

	public abstract class FrontViewBase<TView, TModel>
		where TView : FrontViewBase<TView, TModel>, new()
	{
		public const string DefaultMappingName = "default";

		private static readonly IReadOnlyDictionary<MappingDirection, IReadOnlyDictionary<string, LambdaExpression>> Mappings;
		private static readonly IReadOnlyDictionary<string, Expression<Func<TView, TModel>>> ToModelProjections;
		private static readonly IReadOnlyDictionary<string, Expression<Func<TModel, TView>>> ToViewProjections;
		private static readonly IReadOnlyDictionary<string, Func<TView, TModel>> ToModelDelegates;
		private static readonly IReadOnlyDictionary<string, Func<TModel, TView>> ToViewDelegates;

		static FrontViewBase()
		{
			var registry = new MappingRegistry<TView, TModel>();
			new TView().ConfigureMappings(registry);

			Mappings = registry.Build();
			ToModelProjections = BuildTypedProjectionMap<TView, TModel>(MappingDirection.ToModel);
			ToViewProjections = BuildTypedProjectionMap<TModel, TView>(MappingDirection.ToView);

			ToModelDelegates = CompileProjectionMap(ToModelProjections);
			ToViewDelegates = CompileProjectionMap(ToViewProjections);
		}

		protected abstract void ConfigureMappings(MappingRegistry<TView, TModel> registry);

		public static Expression<Func<TView, TModel>> GetToModelProjection(string mappingName = DefaultMappingName) =>
			GetMapping(ToModelProjections, MappingDirection.ToModel, mappingName);

		public static Expression<Func<TModel, TView>> GetToViewProjection(string mappingName = DefaultMappingName) =>
			GetMapping(ToViewProjections, MappingDirection.ToView, mappingName);

		public static TModel MapToModel(TView view, string mappingName = DefaultMappingName)
		{
			ArgumentNullException.ThrowIfNull(view);
			return GetMapping(ToModelDelegates, MappingDirection.ToModel, mappingName)(view);
		}

		public static TView MapToView(TModel model, string mappingName = DefaultMappingName)
		{
			ArgumentNullException.ThrowIfNull(model);
			return GetMapping(ToViewDelegates, MappingDirection.ToView, mappingName)(model);
		}

		private static IReadOnlyDictionary<string, Expression<Func<TSource, TDestination>>> BuildTypedProjectionMap<TSource, TDestination>(
			MappingDirection direction)
		{
			var source = Mappings[direction];
			var typed = new Dictionary<string, Expression<Func<TSource, TDestination>>>(StringComparer.Ordinal);

			foreach (var entry in source)
			{
				typed[entry.Key] = (Expression<Func<TSource, TDestination>>)entry.Value;
			}

			return typed;
		}

		private static IReadOnlyDictionary<string, Func<TSource, TDestination>> CompileProjectionMap<TSource, TDestination>(
			IReadOnlyDictionary<string, Expression<Func<TSource, TDestination>>> projections)
		{
			var delegates = new Dictionary<string, Func<TSource, TDestination>>(StringComparer.Ordinal);

			foreach (var entry in projections)
			{
				delegates[entry.Key] = entry.Value.Compile();
			}

			return delegates;
		}

		private static TValue GetMapping<TValue>(
			IReadOnlyDictionary<string, TValue> source,
			MappingDirection direction,
			string mappingName)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(mappingName);

			if (source.TryGetValue(mappingName, out var mapping))
			{
				return mapping;
			}

			throw new KeyNotFoundException(
				$"No mapping registered for direction '{direction}' with name '{mappingName}'.");
		}
	}
}
