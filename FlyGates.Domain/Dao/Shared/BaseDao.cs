namespace FlyGates.Application.Dao.Shared;


// Data Access Object
public class BaseDao
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public DateTime CreatedAt { get; set; }
	private DateTime _updatedAt;

	public DateTime UpdatedAt
	{
		get
		{
			if (_updatedAt.Year > 2020)
			{
				return _updatedAt;
			}
			
			if (CreatedAt.Year > 2020)
			{
				return this.CreatedAt;
			}

			return new DateTime(2002, 4, 28);
		}
		set
		{
			// Ao definir UpdatedAt, o valor é armazenado no campo de suporte
			_updatedAt = value;
		}
	}
}
