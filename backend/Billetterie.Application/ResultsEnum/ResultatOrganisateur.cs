using Billetterie.Domain.Entities;


namespace Billetterie.Application.ResultsEnum
{
    public class ResultatOrganisateur
    {
        public bool Succes { get; }
        public Organisateur? Organisateur { get; }
        public MotifEchecOrganisateurEnum? Motif { get; }

        private ResultatOrganisateur(bool succes, Organisateur? organisateur, MotifEchecOrganisateurEnum? motif)
        {
            Succes = succes;
            Organisateur = organisateur;
            Motif = motif;
        }

        public static ResultatOrganisateur Reussi(Organisateur organisateur)
            => new(true, organisateur, null);

        public static ResultatOrganisateur Echec(MotifEchecOrganisateurEnum motif)
            => new(false, null, motif);
    }
}
