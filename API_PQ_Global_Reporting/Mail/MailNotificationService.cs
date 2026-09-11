using API_PQ_Global_Reporting.Mail.Services;
using API_PQ_Global_Reporting.Models;
using API_PQ_Global_Reporting.Utils;
using Microsoft.Extensions.Options;

namespace API_PQ_Global_Reporting.Mail
{
    public class MailNotificationService
    {
        private readonly string _connectionString;
        private readonly SmtpSettings _smtpSettings;
        private readonly SystemSettings _systemSettings;
        private readonly AssignedToMailService _assignedToMailService;
        private readonly MfgApprovalMailService _mfgApprovalMailService;
        private readonly MfgRejectionMailService _mfgRejectionMailService;
        private readonly EffectivenessMonitoringMailService _effectivenessMonitoringMailService;
        private readonly EffectivenessRejectionMailService _effectivenessRejectionMailService;
        private readonly QualityManagerApprovalMailService _qualityManagerApprovalMailService;
        private readonly QualityManagerRejectionMailService _qualityManagerRejectionMailService;
        private readonly McarClosedMailService _mcarClosedMailService;

        public MailNotificationService(
            IConfiguration configuration,
            IOptions<SmtpSettings> smtpSettings,
            IOptions<SystemSettings> systemSettings)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string not found");
            _smtpSettings = smtpSettings.Value;
            _systemSettings = systemSettings.Value;

            // Initialize mail services
            _assignedToMailService = new AssignedToMailService(_connectionString, _smtpSettings, _systemSettings);
            _mfgApprovalMailService = new MfgApprovalMailService(_connectionString, _smtpSettings, _systemSettings);
            _mfgRejectionMailService = new MfgRejectionMailService(_connectionString, _smtpSettings, _systemSettings);
            _effectivenessMonitoringMailService = new EffectivenessMonitoringMailService(_connectionString, _smtpSettings, _systemSettings);
            _effectivenessRejectionMailService = new EffectivenessRejectionMailService(_connectionString, _smtpSettings, _systemSettings);
            _qualityManagerApprovalMailService = new QualityManagerApprovalMailService(_connectionString, _smtpSettings, _systemSettings);
            _qualityManagerRejectionMailService = new QualityManagerRejectionMailService(_connectionString, _smtpSettings, _systemSettings);
            _mcarClosedMailService = new McarClosedMailService(_connectionString, _smtpSettings, _systemSettings);
        }

        public async Task<bool> SendMailForConcernAsync(decimal concernId)
        {
            try
            {
                return await _assignedToMailService.SendMailAsync(concernId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending concern assignment mail: {ex.Message}", ex);
            }
        }

        public async Task<bool> SendMailForSubmissionAsync(decimal concernId)
        {
            try
            {
                return await _mfgApprovalMailService.SendMailAsync(concernId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending concern submission mail: {ex.Message}", ex);
            }
        }

        public async Task<bool> SendMailForEffectivenessMonitoringAsync(decimal concernId)
        {
            try
            {
                return await _effectivenessMonitoringMailService.SendMailAsync(concernId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending effectiveness monitoring mail: {ex.Message}", ex);
            }
        }

        public async Task<bool> SendMailForEffectivenessRejectionAsync(decimal concernId)
        {
            try
            {
                return await _effectivenessRejectionMailService.SendMailAsync(concernId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending effectiveness rejection mail: {ex.Message}", ex);
            }
        }

        public async Task<bool> SendMailForQualityManagerApprovalAsync(decimal concernId)
        {
            try
            {
                return await _qualityManagerApprovalMailService.SendMailAsync(concernId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending quality manager approval mail: {ex.Message}", ex);
            }
        }

        public async Task<bool> SendMailForMfgRejectionAsync(decimal concernId)
        {
            try
            {
                return await _mfgRejectionMailService.SendMailAsync(concernId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending manufacturing rejection mail: {ex.Message}", ex);
            }
        }

        public async Task<bool> SendMailForQualityManagerRejectionAsync(decimal concernId)
        {
            try
            {
                return await _qualityManagerRejectionMailService.SendMailAsync(concernId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending quality manager rejection mail: {ex.Message}", ex);
            }
        }

        public async Task<bool> SendMailForMcarClosedAsync(decimal concernId)
        {
            try
            {
                return await _mcarClosedMailService.SendMailAsync(concernId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending MCAR closed mail: {ex.Message}", ex);
            }
        }

        // LogMailEventAsync
        
      public async Task<bool> LogMailEventAsync(string controller)
        {
            try
            {
                 await _mcarClosedMailService.LogMailEventAsync("", "", "Sending mail error form controller " +controller, "Failed", "Error message from controller");
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending MCAR closed mail: {ex.Message}", ex);
            }
        }
    }
}
