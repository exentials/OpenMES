using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.Data.Dtos.Resources;

namespace OpenMES.WebAdmin.Components.Common
{
	public static class EnumTextLocalizer
	{
		public static string Localize<TEnum>(TEnum value) where TEnum : struct, Enum
			=> Localize((Enum)(object)value);

		public static string Localize(Enum value)
		{
			var key = $"{value.GetType().Name}_{value}";
			return DtoResources.ResourceManager.GetString(key, DtoResources.Culture) ?? value.ToString();
		}
	}

	public class PropertyColumnExt<TGridItem, TProp> : PropertyColumn<TGridItem,TProp>
	{

	protected override void OnParametersSet()
	{
		if (Title is null && Property.Body is MemberExpression memberExpression)
		{
			var memberInfo = memberExpression.Member;
			var displayName = memberInfo?.GetCustomAttribute<DisplayNameAttribute>();
			var display     = memberInfo?.GetCustomAttribute<DisplayAttribute>();

			// display.GetName() resolves the localized string via ResourceType;
			// display.Name alone returns only the resource key.
			Title = displayName?.DisplayName
			     ?? display?.GetName()
			     ?? memberInfo?.Name
			     ?? "";
		}
		base.OnParametersSet();
	}
		
	}
}
